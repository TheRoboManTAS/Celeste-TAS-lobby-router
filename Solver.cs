using System.Numerics;

namespace RoboRouter;

public sealed class Solver
{
    public const int NoSolutionTime = 99999999;
    const int Inf = int.MaxValue / 4;

    readonly int nodeCount, start, finish, restartPenalty, maxRestarts;
    readonly bool requiredRestarts, infRestarts;
    readonly int[][] targets;
    readonly int[][] times;
    readonly ulong[] targeterMask;
    readonly int[] lowestTimes;
    readonly ulong allVisited;

    ulong visited;
    int restartCount;
    int index;
    readonly int[] trail;
    int unvisitedLowerBound;
    int cutoff;

    Solution[] solutions = Array.Empty<Solution>();
    int topN;
    Action<Solution>? onNewFastest;

    readonly TranspositionTable table;
    [ThreadStatic] static TranspositionTable? sharedTable;

    readonly int[] localId;

    public long Iterations;
    public long CutBranches;
    public int ConsideredSolutions;
    SharedBoundTable? shared;
    ulong shareMask;
    int shareForbidden = -1;
    ulong[] canonicalBit = Array.Empty<ulong>();
    int[] canonicalId = Array.Empty<int>();
    ulong canonicalVisited;

    public void ShareBounds(SharedBoundTable table, int[] canonicalIds, ulong shareMask, int shareForbidden)
    {
        shared = table;
        canonicalId = canonicalIds;
        canonicalBit = canonicalIds.Select(id => 1UL << id).ToArray();
        this.shareMask = shareMask;
        this.shareForbidden = shareForbidden;
    }

    public Solver(PlaceInfo[] nodes, int start, int finish, int restartPenalty, int maxRestarts,
        bool requiredRestarts, int tableSizeLog2 = 20)
    {
        nodeCount = nodes.Length;
        if (nodeCount > 64)
            throw new NotSupportedException("The router currently only supports at most 64 nodes, because of 64-bit trickery™.");

        this.start = start;
        this.finish = finish;
        this.restartPenalty = restartPenalty;
        this.maxRestarts = maxRestarts;
        this.requiredRestarts = requiredRestarts;
        infRestarts = maxRestarts < 0;

        targets = nodes.Select(p => p.targets).ToArray();
        times = nodes.Select(p => p.times).ToArray();

        targeterMask = new ulong[nodeCount];
        for (int i = 0; i < nodeCount; i++)
            foreach (int t in targets[i])
                targeterMask[t] |= 1UL << i;

        // Determine lowest incoming time for each node
        lowestTimes = Enumerable.Repeat(NoSolutionTime, nodeCount).ToArray();
        lowestTimes[start] = 0;
        for (int i = 0; i < nodeCount; i++)
            for (int t = 0; t < targets[i].Length; t++)
                lowestTimes[targets[i][t]] = Math.Min(lowestTimes[targets[i][t]], times[i][t]);

        for (int i = 0; i < nodeCount; i++)
            if (i != start)
                allVisited |= 1UL << i;

        trail = new int[nodeCount + targets[start].Length];

        int maxArcs = targets.Sum(t => t.Length) + targets[start].Length;
        localId = new int[nodeCount];

        arcTail = new int[maxArcs];
        arcHead = new int[maxArcs];
        arcCost = new int[maxArcs];
        arcSlot = new int[maxArcs];
        inStart = new int[nodeCount + 1];
        inTail = new int[maxArcs];
        inHead = new int[maxArcs];
        inCost = new int[maxArcs];
        inSlot = new int[maxArcs];
        inTime = new int[maxArcs];
        label = new int[nodeCount];
        offset = new int[nodeCount];
        memberNext = new int[nodeCount];
        memberHead = new int[2 * nodeCount];
        memberTail = new int[2 * nodeCount];
        cheapestIn = new int[2 * nodeCount];
        parentCycle = new int[2 * nodeCount];
        mark = new int[2 * nodeCount];
        chain = new int[2 * nodeCount];
        treeIn = new int[2 * nodeCount];
        subgradient = new int[nodeCount + 1];
        outDegree = new int[nodeCount + 1];
        lambda = new int[nodeCount];
        localLambda = new int[nodeCount + 1];
        capacity = new int[nodeCount + 1];
        localPlace = new int[nodeCount];
        rootArcReduced = new int[maxArcs];
        int maxTargets = targets.Max(t => t.Length);
        childBounds = Enumerable.Range(0, trail.Length).Select(_ => new int[maxTargets]).ToArray();
        maxArcTime = Math.Max(restartPenalty, times.SelectMany(t => t).DefaultIfEmpty(0).Max()) + restartPenalty;

        // reuse the table of this thread for testing new connections
        if (sharedTable == null || sharedTable.SizeLog2 != tableSizeLog2)
            sharedTable = new TranspositionTable(tableSizeLog2);
        table = sharedTable;
    }

    // returns the topN fastest routes, unfilled places have an empty path
    public Solution[] Solve(int topN, Action<Solution>? onNewFastest = null)
    {
        this.topN = topN;
        this.onNewFastest = onNewFastest;
        solutions = Enumerable.Repeat(new Solution(Array.Empty<int>(), NoSolutionTime), topN).ToArray();
        cutoff = NoSolutionTime;

        visited = 0;
        canonicalVisited = 0;
        restartCount = 0;
        index = 0;
        unvisitedLowerBound = 0;
        for (int i = 0; i < nodeCount; i++)
            if (i != start)
                unvisitedLowerBound += lowestTimes[i];

        if (lowestTimes.Any(t => t >= NoSolutionTime))
            return solutions;

        table.NextGeneration();

        if (topN == 1 && onNewFastest == null)
            SearchRisingCutoff();
        else
            Search(start, 0);
        return solutions;
    }

    // Instead of starting from a practically infinite upper bound, this finds a good lower bound and looks up from there.
    // Skips having to look at a bunch of bad initial solutions which is useful when we only care about the best solution, and not the best N.
    void SearchRisingCutoff()
    {
        // the search starts just above this bound, so a closer to optimal lower bound is worth the extra effort here
        int bound = LowerBound(start, NoSolutionTime, FirstBoundRounds, true);
        if (bound >= Inf)
            return;

        bound = Math.Max(bound, LowerBound(start, NoSolutionTime, FirstBoundRounds, true));
        int step = Math.Max(1, bound / 100);
        int roundCutoff = Math.Min(bound + 1, NoSolutionTime);
        while (true) {
            solutions[0] = new Solution(Array.Empty<int>(), roundCutoff);
            cutoff = roundCutoff;
            int result = Search(start, 0);
            if (solutions[0].path.Length > 0 || roundCutoff >= NoSolutionTime || result >= Inf)
                break;
            roundCutoff = (int)Math.Min(NoSolutionTime, Math.Max(result + 1L, (long)roundCutoff + step));
            step = (int)Math.Min(NoSolutionTime, 2L * step);
        }
        if (solutions[0].path.Length == 0)
            solutions[0] = new Solution(Array.Empty<int>(), NoSolutionTime);
    }

    // returns a lower bound of the time still needed to finish from here
    int Search(int pos, int time)
    {
        trail[index] = pos;
        Iterations++;

        if (pos == finish) {
            if (visited == allVisited) {
                AddSolution(time);
                return 0;
            }
            return Inf;
        }

        // the cheapest entrance of every unvisited place as an optimistic lower bound
        int bound = unvisitedLowerBound;
        if (time + bound >= cutoff) {
            CutBranches++;
            return bound;
        }

        int stateKey = pos + 1 + (infRestarts ? 0 : restartCount << 8);
        int known = table.Get(visited, stateKey);
        bool share = shared != null && (visited & shareMask) == shareMask && pos != shareForbidden;
        int sharedKey = 0;
        if (share) {
            sharedKey = canonicalId[pos] + 1 + (infRestarts ? 0 : restartCount << 8);
            int sharedKnown = shared!.Get(canonicalVisited, sharedKey);
            if (sharedKnown > known)
                known = sharedKnown;
        }
        bool boundComputed = known == 0;
        if (boundComputed)
            known = LowerBound(pos, cutoff - time);
        if (known > bound) {
            bound = known;
            if (time + bound >= cutoff) {
                CutBranches++;
                table.Set(visited, stateKey, bound);
                if (share)
                    shared!.Set(canonicalVisited, sharedKey, bound);
                return bound;
            }
        }

        var posTargets = targets[pos];
        var posTimes = times[pos];

        var childBound = childBounds[index];
        bool haveChildBounds = boundComputed && reducedValid;
        if (haveChildBounds) {
            int arc = 0;
            for (int i = 0; i < posTargets.Length; i++)
                if ((visited & (1UL << posTargets[i])) == 0)
                    childBound[i] = lastRoundBound + rootArcReduced[arc++];
        }

        // a still unvisited node that can't be entered from anywhere else has to be the next in the route
        // if more than 1 node has only the current one as an entry, then the route is impossible
        int deadEnd = -1;
        for (int i = 0; i < posTargets.Length; i++) {
            int target = posTargets[i];
            if ((visited & (1UL << target)) == 0 && (targeterMask[target] & ~visited) == 0) {
                if (deadEnd >= 0) {
                    table.Set(visited, stateKey, Inf);
                    if (share)
                        shared!.Set(canonicalVisited, sharedKey, Inf);
                    return Inf;
                }
                deadEnd = i;
            }
        }

        int result;
        if (deadEnd >= 0) {
            if (haveChildBounds && time + childBound[deadEnd] >= cutoff) {
                result = childBound[deadEnd];
            }
            else {
                result = Move(posTargets[deadEnd], posTimes[deadEnd], time);
            }
        }
        else {
            result = Inf;
            bool mustRestart = true;
            for (int i = 0; i < posTargets.Length; i++) {
                if ((visited & (1UL << posTargets[i])) == 0) {
                    mustRestart = false;
                    if (haveChildBounds && time + childBound[i] >= cutoff) {
                                result = Math.Min(result, childBound[i]);
                        continue;
                    }
                    result = Math.Min(result, Move(posTargets[i], posTimes[i], time));
                }
            }

            if (pos != start && (infRestarts || restartCount < maxRestarts) && (mustRestart || !requiredRestarts)) {
                index++;
                restartCount++;
                result = Math.Min(result, restartPenalty + Search(start, time + restartPenalty));
                restartCount--;
                index--;
            }
        }

        result = Math.Clamp(result, bound, Inf);
        table.Set(visited, stateKey, result);
        if (share)
            shared!.Set(canonicalVisited, sharedKey, result);
        return result;
    }

    int Move(int target, int frames, int time)
    {
        ulong bit = 1UL << target;
        visited |= bit;
        if (shared != null)
            canonicalVisited |= canonicalBit[target];
        unvisitedLowerBound -= lowestTimes[target];
        index++;

        int result = frames + Search(target, time + frames);

        index--;
        unvisitedLowerBound += lowestTimes[target];
        visited &= ~bit;
        if (shared != null)
            canonicalVisited &= ~canonicalBit[target];
        return result;
    }

    void AddSolution(int time)
    {
        ConsideredSolutions++;
        if (time >= cutoff)
            return;

        for (int i = 0; i < topN; i++) {
            if (time < solutions[i].time) {
                var solution = new Solution(trail[..(index + 1)], time);
                if (i == 0)
                    onNewFastest?.Invoke(solution);
                Array.Copy(solutions, i, solutions, i + 1, topN - i - 1);
                solutions[i] = solution;
                break;
            }
        }
        cutoff = solutions[topN - 1].time;
    }

    // this was the most balanced round count based on benchmarks
    const int BoundRounds = 3;
    const int FirstBoundRounds = 30;

    readonly int[] arcTail, arcHead, arcCost, arcSlot;
    readonly int[] inStart, inTail, inHead, inCost, inSlot, inTime, label, offset, memberNext, memberHead, memberTail, cheapestIn, parentCycle, mark, chain, treeIn;
    readonly int[] subgradient, outDegree, lambda, localLambda, capacity, localPlace;
    readonly int maxArcTime;
    readonly int[] rootArcReduced;
    readonly int[][] childBounds;
    int posArcCount, lastRoundBound;
    bool reducedValid;
    int rootLambda, restartLambda;

    // this calculates the cheapest tree from the current position to all unvisited places, which is a better estimate than sum of cheapest entries
    int LowerBound(int pos, int enough, int rounds = BoundRounds, bool shrinkStep = false)
    {
        ulong unvisited = allVisited & ~visited;
        bool restartsLeft = infRestarts || restartCount < maxRestarts;

        if (!infRestarts) {
            ulong usableNotStart = (~visited | (1UL << pos)) & ~(1UL << start);
            int startOnly = 0;
            for (ulong m = unvisited; m != 0; m &= m - 1)
                if ((targeterMask[BitOperations.TrailingZeroCount(m)] & usableNotStart) == 0)
                    startOnly++;
            if ((pos == start ? startOnly - 1 : startOnly) > maxRestarts - restartCount)
                return Inf;
        }

        int n = 1;
        localId[pos] = 0;
        localPlace[0] = pos;
        for (ulong m = unvisited; m != 0; m &= m - 1) {
            int place = BitOperations.TrailingZeroCount(m);
            localPlace[n] = place;
            localId[place] = n++;
        }

        int arcs = AddArcs(pos, unvisited, 0);
        posArcCount = arcs;
        reducedValid = false;
        for (ulong m = unvisited; m != 0; m &= m - 1)
            arcs = AddArcs(BitOperations.TrailingZeroCount(m), unvisited, arcs);
        if (restartsLeft) {
            var startTargets = targets[start];
            var startTimes = times[start];
            for (int i = 0; i < startTargets.Length; i++) {
                if (((unvisited >> startTargets[i]) & 1) != 0) {
                    arcTail[arcs] = 0;
                    arcHead[arcs] = localId[startTargets[i]];
                    arcCost[arcs] = restartPenalty + startTimes[i];
                    arcSlot[arcs] = n;
                    arcs++;
                }
            }
        }

        for (int v = 0; v < n; v++)
            inStart[v] = 0;
        for (int a = 0; a < arcs; a++)
            inStart[arcHead[a]]++;
        for (int v = 1; v < n; v++)
            inStart[v] += inStart[v - 1];
        inStart[n] = arcs;
        for (int a = arcs - 1; a >= 0; a--) {
            int k = --inStart[arcHead[a]];
            inTail[k] = arcTail[a];
            inHead[k] = arcHead[a];
            inCost[k] = arcCost[a];
            inSlot[k] = arcSlot[a];
        }

        int slots = n + 1;
        capacity[0] = 1;
        localLambda[0] = rootLambda;
        capacity[n] = infRestarts ? -1 : maxRestarts - restartCount;
        localLambda[n] = infRestarts ? 0 : restartLambda;
        for (int v = 1; v < n; v++) {
            capacity[v] = localPlace[v] == finish ? 0 : 1;
            localLambda[v] = lambda[localPlace[v]];
        }

        int best = 0;
        double overshoot = 0.1;
        int stalled = 0;
        for (int round = 0; ; round++) {
            int penalty = 0;
            for (int v = 0; v < slots; v++)
                if (capacity[v] > 0)
                    penalty += localLambda[v] * capacity[v];

            bool last = round == rounds - 1;
            int total = Edmonds(n, arcs, enough + penalty, !last);
            if (total >= Inf)
                return Inf;
            int bound = total - penalty;
            if (bound > best) {
                best = bound;
                stalled = 0;
            }
            else if (shrinkStep && ++stalled == 5) {
                overshoot = Math.Max(overshoot / 2, 0.0005);
                stalled = 0;
            }
            lastRoundBound = bound;
            reducedValid = total < enough + penalty;
            if (best >= enough || last || total >= enough + penalty)
                break;

            for (int v = 0; v < slots; v++)
                outDegree[v] = 0;
            for (int v = 1; v < n; v++)
                outDegree[inSlot[treeIn[v]]]++;
            int norm = 0;
            for (int v = 0; v < slots; v++) {
                int g = capacity[v] < 0 ? 0 : outDegree[v] - capacity[v];
                if (g < 0 && localLambda[v] == 0)
                    g = 0;
                subgradient[v] = g;
                norm += g * g;
            }
            if (norm == 0)
                break;

            int target = shrinkStep ? (int)Math.Min(enough, best + best * overshoot + 1) : Math.Min(enough, bound + bound / 10 + 1);
            double step = Math.Max(1.0, (double)(target - bound) / norm);
            for (int v = 0; v < slots; v++)
                localLambda[v] = Math.Clamp(localLambda[v] + (int)Math.Round(step * subgradient[v]), 0, maxArcTime);
        }

        rootLambda = localLambda[0];
        restartLambda = localLambda[n];
        for (int v = 1; v < n; v++)
            lambda[localPlace[v]] = localLambda[v];
        return best;
    }

    // Returns the cost of the cheapest tree rooted at local place 0, or returns early when reaching too high cost
    int Edmonds(int n, int arcs, int enough, bool needTree)
    {
        var lam = localLambda;
        var inStart = this.inStart;
        var inTail = this.inTail;
        var inTime = this.inTime;
        var inCost = this.inCost;
        var inSlot = this.inSlot;
        var label = this.label;
        var offset = this.offset;
        var memberNext = this.memberNext;
        var memberHead = this.memberHead;
        var memberTail = this.memberTail;
        var cheapestIn = this.cheapestIn;
        var parentCycle = this.parentCycle;
        var mark = this.mark;
        var chain = this.chain;

        for (int k = 0; k < arcs; k++)
            inTime[k] = inCost[k] + lam[inSlot[k]];

        for (int v = 0; v < n; v++) {
            label[v] = v;
            offset[v] = 0;
            memberHead[v] = v;
            memberTail[v] = v;
            memberNext[v] = -1;
            mark[v] = -1;
        }
        mark[0] = int.MaxValue;

        int total = 0;
        int nextId = n;
        for (int s = 1; s < n; s++) {
            int u = label[s];
            if (mark[u] != -1)
                continue;
            int length = 0;
            while (true) {
                int best = Inf, bestArc = -1;
                for (int v = memberHead[u]; v >= 0; v = memberNext[v]) {
                    int paid = offset[v];
                    for (int k = inStart[v], end = inStart[v + 1]; k < end; k++) {
                        if (label[inTail[k]] == u)
                            continue;
                        int reduced = inTime[k] - paid;
                        if (reduced < best) {
                            best = reduced;
                            bestArc = k;
                        }
                    }
                }
                if (bestArc < 0)
                    return Inf;
                cheapestIn[u] = bestArc;
                total += best;
                if (total >= enough)
                    return total;
                for (int v = memberHead[u]; v >= 0; v = memberNext[v])
                    offset[v] += best;
                mark[u] = s;
                chain[length++] = u;

                int from = label[inTail[bestArc]];
                if (mark[from] == -1) {
                    u = from;
                    continue;
                }

                if (mark[from] != s)
                    break;

                int cycle = nextId++;
                memberHead[cycle] = -1;
                mark[cycle] = -1;
                int x;
                do {
                    x = chain[--length];
                    parentCycle[x] = cycle;
                    if (memberHead[cycle] < 0)
                        memberHead[cycle] = memberHead[x];
                    else
                        memberNext[memberTail[cycle]] = memberHead[x];
                    memberTail[cycle] = memberTail[x];
                } while (x != from);
                for (int v = memberHead[cycle]; v >= 0; v = memberNext[v])
                    label[v] = cycle;
                u = cycle;
            }
        }

        for (int a = 0; a < posArcCount; a++)
            rootArcReduced[a] = arcCost[a] + lam[arcSlot[a]] - offset[arcHead[a]];

        if (needTree) {
            var treeIn = this.treeIn;
            for (int x = 1; x < nextId; x++)
                treeIn[x] = cheapestIn[x];
            for (int cycle = nextId - 1; cycle >= n; cycle--) {
                int x = inHead[treeIn[cycle]];
                while (parentCycle[x] != cycle)
                    x = parentCycle[x];
                treeIn[x] = treeIn[cycle];
            }
        }
        return total;
    }

    int AddArcs(int place, ulong unvisited, int arcs)
    {
        var placeTargets = targets[place];
        var placeTimes = times[place];
        int from = localId[place];
        for (int i = 0; i < placeTargets.Length; i++) {
            if (((unvisited >> placeTargets[i]) & 1) != 0) {
                arcTail[arcs] = from;
                arcHead[arcs] = localId[placeTargets[i]];
                arcCost[arcs] = placeTimes[i];
                arcSlot[arcs] = from;
                arcs++;
            }
        }
        return arcs;
    }

    sealed class TranspositionTable
    {
        struct Entry
        {
            public ulong Visited;
            public int Key;
            public int Bound;
        }

        readonly Entry[] entries;
        readonly int shift;
        int generation;
        public readonly int SizeLog2;

        public TranspositionTable(int sizeLog2)
        {
            SizeLog2 = sizeLog2;
            entries = new Entry[1 << sizeLog2];
            shift = 64 - sizeLog2;
        }

        public void NextGeneration()
        {
            generation += 1 << 16;
            if (generation >= 1 << 30) {
                Array.Clear(entries);
                generation = 1 << 16;
            }
        }

        int Slot(ulong visited, int key) =>
            (int)(((visited ^ ((ulong)key * 0xC2B2AE3D27D4EB4FUL)) * 0x9E3779B97F4A7C15UL) >> shift);

        public int Get(ulong visited, int key)
        {
            ref var e = ref entries[Slot(visited, key)];
            return e.Key == (key | generation) && e.Visited == visited ? e.Bound : 0;
        }

        public void Set(ulong visited, int key, int bound)
        {
            ref var e = ref entries[Slot(visited, key)];
            e.Visited = visited;
            e.Key = key | generation;
            e.Bound = bound;
        }
    }
}

public sealed class SharedBoundTable
{
    struct Entry
    {
        public ulong Check;
        public ulong Data;
    }

    readonly Entry[] entries;
    readonly int shift;

    public SharedBoundTable(int sizeLog2 = 21)
    {
        entries = new Entry[1 << sizeLog2];
        shift = 64 - sizeLog2;
    }

    int Slot(ulong visited, int key) =>
        (int)(((visited ^ ((ulong)key * 0xC2B2AE3D27D4EB4FUL)) * 0x9E3779B97F4A7C15UL) >> shift);

    public int Get(ulong visited, int key)
    {
        ref var e = ref entries[Slot(visited, key)];
        ulong data = Volatile.Read(ref e.Data);
        ulong check = Volatile.Read(ref e.Check);
        return (check ^ data) == visited && (int)(data >> 32) == key ? (int)(uint)data : 0;
    }

    public void Set(ulong visited, int key, int bound)
    {
        ref var e = ref entries[Slot(visited, key)];
        ulong data = ((ulong)(uint)key << 32) | (uint)bound;
        Volatile.Write(ref e.Data, data);
        Volatile.Write(ref e.Check, visited ^ data);
    }
}

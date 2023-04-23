using System.Text.RegularExpressions;

namespace RoboRouter;
public class AlgRunner
{
    public Settings settings;
    public FileInfo[] files;

    public string startFile;
    public string finishFile;

    public int restartPenalty;

    public bool stillRunning = true;

    string[] places;
    int start;

    public void FetchFileTimes()
    {
        foreach (var file in files) {
            Output.WriteCol(file.start, Color.LawnGreen);
            Output.WriteCol(" to ", Color.White);
            Output.WriteCol(file.end, Color.Violet);
            Output.WriteCol(": ", Color.White);
            Output.WriteCol($"{Misc.AsSeconds(file.time)}({file.time})\n", Color.SkyBlue);
        }
    }

    public void SetAllFileConnectionsToTime(string _start, string _end, int _time) {
        List<FileInfo> matchedFiles;
        if (_start == "0") {
            matchedFiles = files.Where(f => f.end == _end).ToList();
        } else {
            matchedFiles = files.Where(f => f.start == _start || f.end == _end).ToList();
        }
        
        for (int i = 0; i < matchedFiles.Count; i++)
        {
            int matchedIndex = Array.IndexOf(files, matchedFiles[i]);
            FileInfo updatedFile = files[matchedIndex];
            updatedFile.time = _time;
            files[matchedIndex] = updatedFile;
        }
    }

    public void AddNewFileConnection(string _start, string _end, int _time) {
        if (_start == "0") {
            files = files.Prepend(new FileInfo() {start=_start, end=_end, time=_time}).ToArray();
        } else {
            files = files.Append(new FileInfo() {start=_start, end=_end, time=_time}).ToArray();
        }
    }

    public void FilterUnusedFiles() {
        files = files.Where(f => f.time < 10000).ToArray();
    }

    public bool ConnectionExistsInFileInfos(string _start, string _end, FileInfo[] _fileInfos) {
        var matchedFiles = _fileInfos.Where(f => f.start == _start && f.end == _end).ToList();
        return matchedFiles.Count > 0;
    }

    public void EditFilesToTestNewConnection(string _start, string _end) {
        SetAllFileConnectionsToTime(_start, _end, 60000);
        FilterUnusedFiles();
        AddNewFileConnection(_start, _end, 0);
    }

    public FileInfo[] DeepCopyFileInfoArray(FileInfo[] _fileInfos)
    {
        FileInfo[] copiedFiles = _fileInfos.Select(f => new FileInfo
        {
            start = f.start,
            end = f.end,
            time = f.time
        }).ToArray();

        return copiedFiles;
    }

    public void SolveLobby()
    {
        // put the name of every distinct place to an array
        places = files.Select(f => f.start)
            .Concat(files.Select(f => f.end))
            .Distinct().ToArray();
        // get amount of places minus the start, for checking pathfind trail length
        int placeCount = places.Length - 1;

        // parse `files` into a more efficient pathfinding data structure. check PlaceInfo struct.
        PlaceInfo[] nodes = places.Select<string, PlaceInfo>(p => new() {
            name = p,
            targets = files.Where(i => i.start == p).Select(i =>
                    Array.IndexOf(places, i.end)).ToArray(),
            times = files.Where(i => i.start == p).Select(i =>
                    files.First(f => f.start == p && f.end == i.end).time).ToArray()
        }).ToArray();
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].targeters = Enumerable.Range(0, nodes.Length)
                .Where(j => nodes[j].targets.Contains(i)).ToArray();

        start = settings.UseTableInput ? 0 : Array.IndexOf(places, startFile);
        int finish = settings.UseTableInput ? places.Length - 1 : Array.IndexOf(places, finishFile);

        var solutions = new List<Solution>();
        for (int i = 0; i < settings.topNSolutions; i++) {
            solutions.Add(new Solution(new int[] {}, 99999999));
        }

        long iterations = 0;
        int consideredSolutions = 0;
        int cutBranches = 0;
        int restartCount = 0;
        bool infRestarts = settings.maxRestarts < 0;

        var trail = new int[places.Length + nodes[start].targets.Length];
        var canGo = Enumerable.Repeat(true, places.Length).ToArray();
        int index = 0;
        int visitCount = 0;

        Func<int, bool, bool> canRestart;
        if (infRestarts) {
            canRestart = settings.RequiredRestarts
                ? (pos, must) => (pos != start) & must
                : (pos, must) => pos != start;
        }
        else {
            canRestart = settings.RequiredRestarts
                ? (pos, must) => (pos != start) & (restartCount < settings.maxRestarts) & must
                : (pos, must) => (pos != start) & (restartCount < settings.maxRestarts);
        }

        var lowestTimes = new List<int>();
        for (int n = 0; n < nodes.Count(); n++) {
            lowestTimes.Add(99999999);
        }

        // Determine lowest incoming time for each node
        lowestTimes[0] = 0;
        for (int n = 0; n < nodes.Count(); n++) {
            for (int t = 0; t < nodes[n].targets.Count(); t++) {
                if (lowestTimes[nodes[n].targets[t]] > nodes[n].times[t]) {
                    lowestTimes[nodes[n].targets[t]] = nodes[n].times[t];
                }
            }
        }

        int globalLowerBound = 0;

        for (int i = 0; i < lowestTimes.Count; i++) {
            globalLowerBound += lowestTimes[i];
        }

        int localLowerBound = globalLowerBound;

        Console.WriteLine("Current fastest route:");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        
        PathFind(start);
        void PathFind(int pos)
        {
            trail[index] = pos;

            iterations++;

            // if finished, process the solution
            if (pos == finish) {
                if (visitCount == placeCount) {
                    var truncated = trail.Take(index + 1).ToArray();
                    int time = truncated.Skip(1).Select((e, i) => e == start ? restartPenalty : nodes[trail[i]].FramesTo(e)).Sum();
                    consideredSolutions++;
                    if (time < solutions[settings.topNSolutions - 1].time) {
                        for (int i = 0; i < settings.topNSolutions; i++) {
                            if (time < solutions[i].time) { 
                                if (i == 0) {
                                    Console.WriteLine(ParseSolution(new Solution(truncated, time)));
                                }
                                solutions.Insert(i, new Solution(truncated, time));
                                solutions.RemoveAt(settings.topNSolutions);
                                break;
                            }
                        }
                    }
                }
                return;
            }

            if (localLowerBound >= solutions[settings.topNSolutions - 1].time) {
                cutBranches++;
                return;
            }

            int addedTime = index != 0 ? trail[index] == start ? restartPenalty : nodes[trail[index - 1]].FramesTo(trail[index]) : 0;
            int updateLowerBound = addedTime - lowestTimes[pos];

            var targets = nodes[pos].targets;

            bool hasDeadEnd = false;
            int deadEnd = 0;
            for (int i = 0; i < targets.Length; i++) {
                int target = targets[i];
                if (canGo[target]) {
                    var node = nodes[target];
                    for (int j = 0; j < node.targeters.Length; j++)
                        if (canGo[node.targeters[j]])
                            goto EndChecking;
                    if (hasDeadEnd)
                        return;
                    hasDeadEnd = true;
                    deadEnd = target;
                }
                EndChecking: { }
            }

            if (hasDeadEnd) {
                visitCount++;
                index++;
                canGo[deadEnd] = false;
                localLowerBound += updateLowerBound;

                PathFind(deadEnd);

                visitCount--;
                index--;
                canGo[deadEnd] = true;
                localLowerBound -= updateLowerBound;
                return;
            }

            // branch out to all current position connections that havent been visited
            bool mustRestart = true;
            for (int i = 0; i < targets.Length; i++) {
                int target = targets[i];
                if (canGo[target]) {
                    visitCount++;
                    index++;
                    canGo[target] = false;
                    localLowerBound += updateLowerBound;

                    PathFind(targets[i]);

                    visitCount--;
                    index--;
                    canGo[target] = true;
                    localLowerBound -= updateLowerBound;

                    mustRestart = false;
                }
            }

            // restarts
            if (canRestart(pos, mustRestart)) {
                index++;
                restartCount++;
                localLowerBound += updateLowerBound;
                PathFind(start);
                localLowerBound -= updateLowerBound;
                restartCount--;
                index--;
            }
        }

        solutions.Reverse();
        timer.Stop();

        int placementPadding = solutions.Count.ToString().Length;
        var bestRestartSolutions = new List<(Solution, int)>();
        for (int i = 0; i < nodes.Length; i++) {
            bestRestartSolutions.Add(new(new Solution(new int[] {}, -1), -1));
        }
        if (!settings.LogResults) {
            Console.WriteLine("\n-- Fastest Routes --");
            int firstRealSolutionIndex = Math.Max(0, solutions.Count - consideredSolutions);
            for (int i = firstRealSolutionIndex; i < solutions.Count; i++) {
                PrintSolution(solutions[i], solutions.Count - i, placementPadding);
                int restarts = solutions[i].path.Where(x => x.Equals(0)).Count() - 1;
                if (restarts >= 0) {
                    bestRestartSolutions[restarts] = new(solutions[i], solutions.Count - i);
                }                
            }
            Console.WriteLine("\n-- Best Restart Solutions --");
            for (int i = 0; i < bestRestartSolutions.Count; i++) {
                if (bestRestartSolutions[i].Item2 > 0) {
                    Console.WriteLine($"With {i} restart(s):");
                    PrintSolution(bestRestartSolutions[i].Item1, bestRestartSolutions[i].Item2, placementPadding);
                }
            }
            Console.WriteLine("No solution with different amounts of restarts found.");
        } else {
            Directory.CreateDirectory("Results");
            var file = "Results\\" + DateTime.Now.ToString().Replace('/', '-').Replace(':', '.') + ".txt";
            File.Create(file).Close();
            File.WriteAllText(file,
                string.Join('\n', solutions.Select(sol => ParseSolution(sol))));
            Console.WriteLine("\nResults logged into " + file);
        }

        PrintSettings();

        Console.WriteLine("\n-- Statistics --");
        Console.WriteLine("Routing took: " + timer.Elapsed);
        Console.WriteLine("Pathfind function calls: "+ iterations);
        Console.WriteLine("Branches cut: " + cutBranches);
        Console.WriteLine("Full solutions calculated: " + consideredSolutions);

        // give debug advice if no solutions
        if (solutions.Count == 0) {
            Output.WriteCol("You got zero solutions. Try running ", Color.Yellow);
            Output.WriteCol("List file times ", Color.LightPink);
            Output.WriteCol("to see if file names were parsed correctly.\n", Color.Yellow);
            Output.SetColor(Color.White);
        }
        // mini menu for printing out an approx draft of finished lobby file
        else if (!settings.LogResults && !settings.UseTableInput) {
            Output.WriteCol("\n\n --- Post Solution Actions ---\n", Color.LimeGreen);
            PrintDrafter(false);

            while (true) {
                Output.inputRow = Console.CursorTop;
                string raw = Console.ReadLine();
                if (!stillRunning)
                    return;
                var input = Misc.ParseInput(raw.ToLower());

                if (input.Item1 == "" || !"de".Contains(input.Item1[0])) {
                    Output.CursorBack();
                    Console.Write(new string(' ', raw.Length));
                    Output.CursorBack();
                    continue;
                }
                else if (input.Item1[0] == 'd') {
                    int.TryParse(input.Item2, out int Index);
                    int[] trl = solutions[^(Index + 1)].path;

                    Output.SetColor(Color.Khaki);

                    Console.WriteLine("\n\n#INSERT TAS START\n");
                    for (int i = 1; i < trl.Length; i++) {
                        if (trl[i] == start) {
                            Console.WriteLine("#Restart\n  31\n   1,Q\n   1,J\n  31\n");
                            i++;
                        }

                        var file = files.First(f => f.start == places[trl[i - 1]] && f.end == places[trl[i]]);

                        Console.WriteLine($"Read,{Misc.fileNameFromDir.Match(file.directory)},Start");
                        Console.WriteLine("#Read,EnterLevel");
                        Console.WriteLine($"Read,{file.end}.tas,Start");
                        Console.WriteLine(i == trl.Length - 1 ?
                            "  59\n   1,O" : "  30\n  34,O\n");
                    }
                }
                // print and explain EnterLevel file
                else if (input.Item1[0] == 'e') {
                    Output.WriteCol("\n\nunsafe\n  20\n   1,J\n  10\n#NoCredits\n  20\n   1,J\n  46\n\n\n", Color.Khaki);
                    Output.SetColor(Color.PeachPuff);
                    Console.WriteLine("It is assumed that your lobby files end with 1,X.");
                    Console.WriteLine("Some maps are missing the credits page, making them faster to enter so\nwhen this is the case, use this suffix: \"Read,EnterLevel,NoCredits\".");
                    Console.WriteLine("The last line of this file might have to be 47 frames instead. I don't know when though.");
                }
                PrintDrafter();
            }
        }
        return;
    }

    public void PrintSettings() {
        Console.WriteLine("\n-- Settings --");
        Console.WriteLine("Only Dead End Restarts: " + settings.RequiredRestarts);
        Console.WriteLine("Max Restart Count: " + settings.maxRestarts);
        Console.WriteLine("Number of Solutions: " + settings.topNSolutions);
        Console.WriteLine("Find New Connections Mode: " + settings.newConnectionsMode);
    }

    public Solution TestConnection()
    {
        // put the name of every distinct place to an array
        places = files.Select(f => f.start)
            .Concat(files.Select(f => f.end))
            .Distinct().ToArray();
        // get amount of places minus the start, for checking pathfind trail length
        int placeCount = places.Length - 1;

        // parse `files` into a more efficient pathfinding data structure. check PlaceInfo struct.
        PlaceInfo[] nodes = places.Select<string, PlaceInfo>(p => new() {
            name = p,
            targets = files.Where(i => i.start == p).Select(i =>
                    Array.IndexOf(places, i.end)).ToArray(),
            times = files.Where(i => i.start == p).Select(i =>
                    files.First(f => f.start == p && f.end == i.end).time).ToArray()
        }).ToArray();
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].targeters = Enumerable.Range(0, nodes.Length)
                .Where(j => nodes[j].targets.Contains(i)).ToArray();

        start = settings.UseTableInput ? 0 : Array.IndexOf(places, startFile);
        int finish = settings.UseTableInput ? places.Length - 1 : Array.IndexOf(places, finishFile);

        var solutions = new List<Solution>();
        solutions.Add(new Solution(new int[] {}, 99999999));

        long iterations = 0;
        int consideredSolutions = 0;
        int cutBranches = 0;
        int restartCount = 0;
        bool infRestarts = settings.maxRestarts < 0;

        var trail = new int[places.Length + nodes[start].targets.Length];
        var canGo = Enumerable.Repeat(true, places.Length).ToArray();
        int index = 0;
        int visitCount = 0;

        Func<int, bool, bool> canRestart;
        if (infRestarts) {
            canRestart = settings.RequiredRestarts
                ? (pos, must) => (pos != start) & must
                : (pos, must) => pos != start;
        }
        else {
            canRestart = settings.RequiredRestarts
                ? (pos, must) => (pos != start) & (restartCount < settings.maxRestarts) & must
                : (pos, must) => (pos != start) & (restartCount < settings.maxRestarts);
        }

        var lowestTimes = new List<int>();
        for (int n = 0; n < nodes.Count(); n++) {
            lowestTimes.Add(99999999);
        }

        // Determine lowest incoming time for each node
        lowestTimes[0] = 0;
        for (int n = 0; n < nodes.Count(); n++) {
            for (int t = 0; t < nodes[n].targets.Count(); t++) {
                if (lowestTimes[nodes[n].targets[t]] > nodes[n].times[t]) {
                    lowestTimes[nodes[n].targets[t]] = nodes[n].times[t];
                }
            }
        }

        int globalLowerBound = 0;

        for (int i = 0; i < lowestTimes.Count; i++) {
            globalLowerBound += lowestTimes[i];
        }

        int localLowerBound = globalLowerBound;

        var timer = System.Diagnostics.Stopwatch.StartNew();
        
        PathFind(start);
        void PathFind(int pos)
        {
            trail[index] = pos;

            iterations++;

            // if finished, process the solution
            if (pos == finish) {
                if (visitCount == placeCount) {
                    var truncated = trail.Take(index + 1).ToArray();
                    int time = truncated.Skip(1).Select((e, i) => e == start ? restartPenalty : nodes[trail[i]].FramesTo(e)).Sum();
                    consideredSolutions++;
                    if (time < solutions[0].time) {
                        solutions.Insert(0, new Solution(truncated, time));
                        solutions.RemoveAt(1);
                    }
                }
                return;
            }

            if (localLowerBound >= solutions[0].time) {
                cutBranches++;
                return;
            }

            int addedTime = index != 0 ? trail[index] == start ? restartPenalty : nodes[trail[index - 1]].FramesTo(trail[index]) : 0;
            int updateLowerBound = addedTime - lowestTimes[pos];

            var targets = nodes[pos].targets;

            bool hasDeadEnd = false;
            int deadEnd = 0;
            for (int i = 0; i < targets.Length; i++) {
                int target = targets[i];
                if (canGo[target]) {
                    var node = nodes[target];
                    for (int j = 0; j < node.targeters.Length; j++)
                        if (canGo[node.targeters[j]])
                            goto EndChecking;
                    if (hasDeadEnd)
                        return;
                    hasDeadEnd = true;
                    deadEnd = target;
                }
                EndChecking: { }
            }

            if (hasDeadEnd) {
                visitCount++;
                index++;
                canGo[deadEnd] = false;
                localLowerBound += updateLowerBound;

                PathFind(deadEnd);

                visitCount--;
                index--;
                canGo[deadEnd] = true;
                localLowerBound -= updateLowerBound;
                return;
            }

            // branch out to all current position connections that havent been visited
            bool mustRestart = true;
            for (int i = 0; i < targets.Length; i++) {
                int target = targets[i];
                if (canGo[target]) {
                    visitCount++;
                    index++;
                    canGo[target] = false;
                    localLowerBound += updateLowerBound;

                    PathFind(targets[i]);

                    visitCount--;
                    index--;
                    canGo[target] = true;
                    localLowerBound -= updateLowerBound;

                    mustRestart = false;
                }
            }

            // restarts
            if (canRestart(pos, mustRestart)) {
                index++;
                restartCount++;
                localLowerBound += updateLowerBound;
                PathFind(start);
                localLowerBound -= updateLowerBound;
                restartCount--;
                index--;
            }
        }

        timer.Stop();

        return solutions[0];
    }

    Regex parseConnectionInput = new(@"^(\s*\d+\s*-\s*\d+\s*)(\s*,\s*\d+\s*-\s*\d+\s*)*$");

    public List<Connection> ParseConnectionInput(string input) {
        var connections = new List<Connection>();

        if (!parseConnectionInput.Match(input).Success) {
            Console.WriteLine($"Warning: Failed to parse input numbers ({input}). Make sure to use the correct format: e.g. 13-18, 0-20, ...");
            return connections;
        }

        string cleanedInput = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        string[] connectionStrings = cleanedInput.Split(',');

        foreach (string connectionString in connectionStrings)
        {
            string[] startAndEnd = connectionString.Split('-');
            int start;
            int end;
            if (!(Array.Exists(places, element => element == startAndEnd[0]) && Array.Exists(places, element => element == startAndEnd[1]))) {
                Console.WriteLine($"Warning: Start or end of connection doesn't exist ({connectionString}). Skipping provided connection.");
                continue;
            }
            if (!int.TryParse(startAndEnd[0], out start))
            {
                Console.WriteLine($"Warning: Failed to parse one of the input numbers ({startAndEnd[0]}). Skipping rest of Input.");
                return connections;
            }
            if (!int.TryParse(startAndEnd[1], out end))
            {
                Console.WriteLine($"Warning: Failed to parse one of the input numbers ({startAndEnd[1]}). Skipping rest of Input.");
                return connections;
            }
            connections.Add(new Connection(start, end));
        }

        return connections;
    }

    public void FindNewConnections() {
        FileInfo[] originalFileArray = DeepCopyFileInfoArray(files);
        Output.SetColor(Color.White);
        Console.WriteLine("-- Find New Connections Mode --");
        Console.WriteLine("Solving lobby to get reference solution...");
        Solution bestSolution = TestConnection();
        Console.WriteLine("\nReference Solution: ");
        PrintSolution(bestSolution);
        Console.WriteLine();

        var connections = new List<Connection>();
        string input = settings.NewConnectionsInput;
        if (!(input.StartsWith("Format:") || input.Length <= 0)) {
            connections = ParseConnectionInput(input);
        } else {
            // Test all connections on empty input
            Console.WriteLine("Test all connections on empty input");
            for (int testStart = 0; testStart < places.Length - 1; testStart++) {
                for (int testEnd = 1; testEnd < places.Length; testEnd++) {
                    connections.Add(new Connection(testStart, testEnd));
                }
            }
        }

        if (connections.Count == 0) {
            Console.WriteLine("No connections to test.");
            return;
        }

        Console.WriteLine("Connections to test: " + connections.Count);

        var usefulConnections = new List<ConnectionResult>();
        int frameDifferenceThreshold = 0;

        foreach (Connection connection in connections) {
            string connectionName = connection.start + "-" + connection.end;
            
            // Don't test invalid connections
            if (connection.end == connection.start || connection.start == 0 && connection.end == places.Length) {
                Console.WriteLine($"\nSkipping test for {connectionName}, Reason: invalid");
                continue;
            }
            // Don't test connections that are part of the input files
            if (ConnectionExistsInFileInfos(connection.start.ToString(), connection.end.ToString(), originalFileArray)) {
                Console.WriteLine($"\nSkipping test for {connectionName}, Reason: Part of input");
                continue;
            }

            Console.WriteLine($"\n-- Testing new connection: {connectionName} --");
            files = DeepCopyFileInfoArray(originalFileArray);
            EditFilesToTestNewConnection(connection.start.ToString(), connection.end.ToString());
            Solution testSolution = TestConnection();
            if (testSolution.path.Length == 0) {
                Console.WriteLine($"Connection not useful, because no route was found containing the connection.");
                continue;
            }
            int frameDifference = bestSolution.time - testSolution.time;
            if (frameDifference < frameDifferenceThreshold) {
                Console.WriteLine($"Connection not useful, because it would need to be {frameDifference}f (or faster) to match (or beat) current best solution.");
            } else {
                usefulConnections.Add(new ConnectionResult(connectionName, ParseSolution(testSolution), frameDifference));
                Console.WriteLine($"Best route using tested connection (assuming 0f for {connectionName}):");
                PrintSolution(testSolution);
                Console.WriteLine($"Connection {connectionName} needs to be {frameDifference}f (or faster) to match (or beat) current best solution.");
            }
        }
        
        Console.WriteLine("\n-- Overview of all potentially useful new connections --\n");

        foreach (ConnectionResult connectionResult in usefulConnections) {
            var split = connectionResult.parsedSol.Split(':');
            Output.WriteCol($"{connectionResult.connectionName})".PadLeft(6), Color.Gray);
            Output.WriteCol($"[{connectionResult.timeNeeded}f]:".PadLeft(8), Color.LightSkyBlue);
            var routePieces = split[1].Split("[R]");
            for (int p = 0; p < routePieces.Count(); p++) {
                Output.WriteCol(routePieces[p], Color.White);
                if (p < routePieces.Count() - 1) {
                    Output.WriteCol("[R]", Color.Orange);    
                }                
            }
            Console.WriteLine();
        }

        PrintSettings();
    }

    string ParseSolution(Solution sol) =>
            $"{Misc.AsSeconds(sol.time)}({sol.time}): {string.Join(", ", sol.path.Skip(1).Select(p => (p == start ? "[R]" : places[p])))}";
        
    void PrintSolution(Solution sol, int placement=0, int placementPadding=1) {
        var split = ParseSolution(sol).Split(':');
        if (placement != 0)
            Output.WriteCol(placement.ToString().PadLeft(placementPadding) + ") ", Color.Gray);
        Output.WriteCol(split[0] + ':', Color.LightSkyBlue);
        var routePieces = split[1].Split("[R]");
        for (int p = 0; p < routePieces.Count(); p++) {
            Output.WriteCol(routePieces[p], Color.White);
            if (p < routePieces.Count() - 1) {
                Output.WriteCol("[R]", Color.Orange);    
            }                
        }
        Console.WriteLine();
    }

    private void PrintDrafter(bool separatorLine = true)
    {
        if (separatorLine)
            Output.WriteCol("-----------------------------------", Color.Lime);
        Output.WriteCol("\nd: ", Color.Yellow);
        Output.WriteCol("Print approximated draft for final main TAS file\n", Color.LightSteelBlue);
        Output.WriteCol("d {0 index}: ", Color.Yellow);
        Output.WriteCol("Pick which route to print the draft of\n", Color.LightSteelBlue);
        Output.WriteCol("e: ", Color.Yellow);
        Output.WriteCol("Print and explain the \"ExitLevel\" inputs if you need but dont have them\n", Color.LightSeaGreen);
        Output.WriteCol(">>> ", Color.White);
    }

    public AlgRunner(MainForm src)
    {
        files = src.files;
        settings = src.settings;
        startFile = src.startFile;
        finishFile = src.finishFile;
        restartPenalty = settings.UseTableInput ? src.tableRestartPenalty : settings.restartPenalty;
    }
}

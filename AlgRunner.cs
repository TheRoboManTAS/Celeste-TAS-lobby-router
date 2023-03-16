namespace RoboRouter;
public class AlgRunner
{
    public Settings settings;
    public FileInfo[] files;

    public string startFile;
    public string finishFile;

    public int restartPenalty;

    public bool stillRunning = true;

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

    public void SolveLobby()
    {
        // put the name of every distinct place to an array
        string[] places = files.Select(f => f.start)
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

        int start = settings.UseTableInput ? 0 : Array.IndexOf(places, startFile);
        int finish = settings.UseTableInput ? places.Length - 1 : Array.IndexOf(places, finishFile);

        var solutions = new List<(int[], int)>();

        long iterations = 0;
        int consideredSolutions = 0;
        int restartCount = 0;
        bool infRestarts = settings.maxRestarts < 0;

        var trail = new int[places.Length + nodes[start].targets.Length];
        var canGo = Enumerable.Repeat(true, places.Length).ToArray();
        int index = 0;
        int visitCount = 0;

        const int topNSolutions = 10;

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
                    solutions.Add(new(truncated, time));
                    if (consideredSolutions > topNSolutions) {
                        solutions.Sort((x, y) => y.Item2.CompareTo(x.Item2));
                        solutions.RemoveAt(0);
                    }
                }
                return;
            }

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

                PathFind(deadEnd);

                visitCount--;
                index--;
                canGo[deadEnd] = true;
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

                    PathFind(targets[i]);

                    visitCount--;
                    index--;
                    canGo[target] = true;

                    mustRestart = false;
                }
            }

            // restarts
            if (canRestart(pos, mustRestart)) {
                index++;
                restartCount++;
                PathFind(start);
                restartCount--;
                index--;
            }
        }

        timer.Stop();


        //if (!settings.DisableSorting)
        //    solutions = solutions.OrderByDescending(s => s.Item2).ToList();
        // normal console output
        if (!settings.LogResults)
            foreach (var sol in solutions) {
                var split = ParseSolution(sol).Split(':');
                Output.WriteCol(split[0] + ':', Color.LightSkyBlue);
                Output.WriteCol(split[1] + '\n', Color.White);
            }
        else {
            Directory.CreateDirectory("Results");
            var file = "Results\\" + DateTime.Now.ToString().Replace('/', '-').Replace(':', '.') + ".txt";
            File.Create(file).Close();
            File.WriteAllText(file,
                string.Join('\n', solutions.Select(sol => ParseSolution(sol))));
            Console.WriteLine("\nResults logged into " + file + "\n");
        }

        Console.WriteLine("\nRouting took: " + timer.Elapsed);
        Console.WriteLine("Pathfind function calls: "+ iterations);
        Console.WriteLine("Solutions considered: " + consideredSolutions);
        Console.WriteLine("Solutions displayed: " + solutions.Count);
        Console.WriteLine("\n-- Settings used --");
        Console.WriteLine("Only Dead End Restarts: " + settings.RequiredRestarts);
        Console.WriteLine("Max Restart Count: " + settings.maxRestarts);


        string ParseSolution((int[] route, int f) sol) =>
            $"{Misc.AsSeconds(sol.f)}({sol.f}) with: {string.Join(", ", sol.route.Skip(1).Select(p => (p == start ? "RESTART" : places[p])))}";


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
                    int[] trl = solutions[^(Index + 1)].Item1;

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

using System.Text.RegularExpressions;

namespace RoboRouter;
public class AlgRunner
{
    public Settings settings;
    public FileInfo[] files;

    public string startFile;
    public string finishFile;

    public int restartPenalty;

    public volatile bool stillRunning = true;

    public int threads;
    Solution testSolution;

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
        for (int i = 0; i < files.Length; i++) {
            if (files[i].end == _end || (_start != "0" && files[i].start == _start))
                files[i].time = _time;
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
        var totalTimer = System.Diagnostics.Stopwatch.StartNew();
        var solver = CreateSolver(out var nodes);
        if (solver == null)
            return;

        Console.WriteLine("Current fastest route:");
        var timer = System.Diagnostics.Stopwatch.StartNew();

        var solutions = solver.Solve(settings.topNSolutions, sol => Console.WriteLine(ParseSolution(sol))).ToList();
        long iterations = solver.Iterations;
        long cutBranches = solver.CutBranches;
        int consideredSolutions = solver.ConsideredSolutions;

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
        Console.WriteLine("Total time: " + totalTimer.Elapsed);

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

    // parse `files` into a more efficient pathfinding data structure. check PlaceInfo struct.
    Solver? CreateSolver(out PlaceInfo[] nodes)
    {
        // put the name of every distinct place to an array
        var placeIndex = new Dictionary<string, int>();
        foreach (var f in files)
            placeIndex.TryAdd(f.start, placeIndex.Count);
        foreach (var f in files)
            placeIndex.TryAdd(f.end, placeIndex.Count);
        places = placeIndex.Keys.ToArray();

        var firstTime = new Dictionary<(string, string), int>();
        foreach (var f in files)
            firstTime.TryAdd((f.start, f.end), f.time);
        var targets = places.Select(_ => new List<int>()).ToArray();
        var times = places.Select(_ => new List<int>()).ToArray();
        foreach (var f in files) {
            targets[placeIndex[f.start]].Add(placeIndex[f.end]);
            times[placeIndex[f.start]].Add(firstTime[(f.start, f.end)]);
        }

        var placeInfos = new PlaceInfo[places.Length];
        for (int i = 0; i < places.Length; i++)
            placeInfos[i] = new PlaceInfo { name = places[i], targets = targets[i].ToArray(), times = times[i].ToArray() };
        var targeters = places.Select(_ => new List<int>()).ToArray();
        for (int j = 0; j < places.Length; j++)
            foreach (int t in placeInfos[j].targets.Distinct())
                targeters[t].Add(j);
        for (int i = 0; i < places.Length; i++)
            placeInfos[i].targeters = targeters[i].ToArray();
        nodes = placeInfos;

        start = settings.UseTableInput ? 0 : Array.IndexOf(places, startFile);
        int finish = settings.UseTableInput ? places.Length - 1 : Array.IndexOf(places, finishFile);

        if (places.Length > 64) {
            Output.PrintError($"The router currently only supports at most 64 nodes, because of 64-bit trickery™, got {places.Length}.\n");
            return null;
        }
        return new Solver(nodes, start, finish, restartPenalty, settings.maxRestarts, settings.RequiredRestarts);
    }

    public Solution TestConnection() =>
        CreateSolver(out _)?.Solve(1)[0] ?? new Solution(new int[] {}, Solver.NoSolutionTime);

    // after both nodes of the test connection have been visited, results from previous runs are shared
    Solution TestConnection(string _start, string _end, SharedBoundTable sharedBounds, Dictionary<string, int> lobbyIds)
    {
        var solver = CreateSolver(out _);
        if (solver == null)
            return new Solution(new int[] {}, Solver.NoSolutionTime);
        if (places.All(lobbyIds.ContainsKey)) {
            int s = Array.IndexOf(places, _start), e = Array.IndexOf(places, _end);
            if (s >= 0 && e >= 0) {
                solver.ShareBounds(sharedBounds, places.Select(p => lobbyIds[p]).ToArray(),
                    (1UL << e) | (s == start ? 0 : 1UL << s), s == start ? -1 : s);
            }
        }
        return solver.Solve(1)[0];
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
        var totalTimer = System.Diagnostics.Stopwatch.StartNew();
        FileInfo[] originalFileArray = DeepCopyFileInfoArray(files);
        Output.SetColor(Color.White);
        Console.WriteLine("-- Find New Connections Mode --");
        Console.WriteLine("Solving lobby to get reference solution...");
        var sharedBounds = new SharedBoundTable();
        Solution bestSolution = new Solution(new int[] {}, Solver.NoSolutionTime);
        var referenceSolver = CreateSolver(out _);
        var lobbyIds = places.Select((p, i) => (p, i)).ToDictionary(x => x.p, x => x.i);
        if (referenceSolver != null) {
            referenceSolver.ShareBounds(sharedBounds, Enumerable.Range(0, places.Length).ToArray(), 0, -1);
            bestSolution = referenceSolver.Solve(1)[0];
        }
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
        var tests = new AlgRunner?[connections.Count];
        var solved = new bool[connections.Count];
        Exception? workerError = null;
        int nextTest = -1;
        void TestWorker()
        {
            try {
                int i;
                while (stillRunning && workerError == null && (i = Interlocked.Increment(ref nextTest)) < connections.Count) {
                    var connection = connections[i];
                    AlgRunner? test = null;
                    if (connection.end != connection.start &&
                        !ConnectionExistsInFileInfos(connection.start.ToString(), connection.end.ToString(), originalFileArray)) {
                        test = new AlgRunner(DeepCopyFileInfoArray(originalFileArray), settings, startFile, finishFile, restartPenalty, 0);
                        test.EditFilesToTestNewConnection(connection.start.ToString(), connection.end.ToString());
                        test.testSolution = test.TestConnection(connection.start.ToString(), connection.end.ToString(), sharedBounds, lobbyIds);
                    }
                    lock (solved) {
                        tests[i] = test;
                        solved[i] = true;
                        Monitor.PulseAll(solved);
                    }
                }
            }
            catch (Exception e) {
                lock (solved) {
                    workerError = e;
                    Monitor.PulseAll(solved);
                }
            }
        }
        var workers = Enumerable.Range(0, Math.Max(1, threads))
            .Select(_ => Task.Factory.StartNew(TestWorker, TaskCreationOptions.LongRunning)).ToArray();

        bool printProgress = settings.PrintDetailedProgress;
        for (int i = 0; i < connections.Count; i++) {
            var connection = connections[i];
            string connectionName = connection.start + "-" + connection.end;

            AlgRunner? test;
            lock (solved) {
                while (!solved[i] && workerError == null && stillRunning)
                    Monitor.Wait(solved, 200);
                if (workerError != null)
                    throw new AggregateException(workerError);
                if (!solved[i])
                    return;
                test = tests[i];
                tests[i] = null;
            }

            // Don't test invalid connections
            if (connection.end == connection.start || connection.start == 0 && connection.end == places.Length) {
                if (printProgress)
                    Console.WriteLine($"\nSkipping test for {connectionName}, Reason: invalid");
                continue;
            }
            // Don't test connections that are part of the input files
            if (test == null) {
                if (printProgress)
                    Console.WriteLine($"\nSkipping test for {connectionName}, Reason: Part of input");
                continue;
            }

            if (printProgress)
                Console.WriteLine($"\n-- Testing new connection: {connectionName} --");
            places = test.places;
            start = test.start;
            Solution testSolution = test.testSolution;
            if (testSolution.path.Length == 0) {
                if (printProgress)
                    Console.WriteLine($"Connection not useful, because no route was found containing the connection.");
                continue;
            }
            int frameDifference = bestSolution.time - testSolution.time;
            if (frameDifference < frameDifferenceThreshold) {
                if (printProgress)
                    Console.WriteLine($"Connection not useful, because it would need to be {frameDifference}f (or faster) to match (or beat) current best solution.");
            } else {
                usefulConnections.Add(new ConnectionResult(connectionName, ParseSolution(testSolution), frameDifference));
                if (printProgress) {
                    Console.WriteLine($"Best route using tested connection (assuming 0f for {connectionName}):");
                    PrintSolution(testSolution);
                    Console.WriteLine($"Connection {connectionName} needs to be {frameDifference}f (or faster) to match (or beat) current best solution.");
                }
            }
        }
        Task.WaitAll(workers);
        
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
        Console.WriteLine("\nTotal time: " + totalTimer.Elapsed);
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
        threads = settings.Multithreading ? Math.Max(1, settings.ThreadCount) : 1;
    }

    AlgRunner(FileInfo[] files, Settings settings, string startFile, string finishFile, int restartPenalty, int _)
    {
        this.files = files;
        this.settings = settings;
        this.startFile = startFile;
        this.finishFile = finishFile;
        this.restartPenalty = restartPenalty;
    }
}

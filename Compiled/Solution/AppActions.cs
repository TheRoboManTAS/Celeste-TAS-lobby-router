using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using static AppSettings;

static class AppActions
{
    public static FileInfo[] files;
    private static int tableRestartPenalty;
    private static int tableFinish;

    public static bool canListOrSolve = false;
    public static void RefreshActionAbility() =>
        canListOrSolve =
                files != null
            && (Settings.useTableInput || ((Settings.lobbyStart ?? new string[] { }).Length == 1 && (Settings.lobbyFinish ?? new string[] { }).Length == 1))
            && !(Settings.useTxtOutput
        && !File.Exists(Settings.outputFile));

    public static void UpdateStartAndEnd()
    {
        if (files != null) {
            Settings.lobbyStart  = files.Where(f => files.All(i => i.end != f.start)).Select(f => f.start).Distinct().ToArray();
            Settings.lobbyFinish = files.Where(f => files.All(i => i.start != f.end)).Select(f => f.end)  .Distinct().ToArray();
            if (Settings.lobbyStart.Length  == 0) Settings.lobbyStart  = null;
            if (Settings.lobbyFinish.Length == 0) Settings.lobbyFinish = null;
        }
    }

    public static void ReparseInput(bool interrupt = true)
    {
        try {
            // parse the files in given directory
            if (!Settings.useTableInput) {
                string sep = Regex.Escape(Settings.nameSeparators);
                Regex parseName = new(@$"(?!.*\\)([^{sep}]+)[{sep}]+([^{sep}]+)\.tas");
                // gets the seconds and decimals of time stamps with flexible formatting
                Regex getTimeStamp = new(@"(?<=#\s*(?:\d+:\s*)?)(\d+[.,]\d+)(?=(?:\(\d*\))?\s*(\n|$))");

                string[] dirs = Directory.GetFiles(Settings.directory);

                files = dirs.Select(
                    file =>
                    {
                        Match nameMatch; try { nameMatch = parseName.Match(file); }
                        catch { throw new Exception($"Could not parse the file name of \"{file}\""); }
                        try {
                            return new FileInfo() {
                                directory = file,
                                start = nameMatch.Groups[1].Value,
                                end = nameMatch.Groups[2].Value,
                                time = (int)Math.Round(
                                    float.Parse(
                                        getTimeStamp.Matches(File.ReadAllText(file)).Last().Value.Replace(',', '.')
                                        , System.Globalization.CultureInfo.InvariantCulture) / 0.017f)
                            };
                        }
                        catch { throw new Exception($"Could not find time stamp in \"{file}\"."); }
                    }).ToArray();
                UpdateStartAndEnd();
            }

            // parse and convert table input into FileInfo[]
            else {
                Regex getNums = new(@"\d+");
                try {
                    // skip first row because input table always starts with '\n' so first elt will be empty
                    // skip last row because we dont account for going from finish to elsewhere
                    var intGrid = Settings.inputTable.Split('\n').Skip(1).SkipLast(1)
                        .Select(ln => getNums.Matches(ln)
                            .Select(m => int.Parse(m.Value))).ToArray();
                    
                    tableRestartPenalty = intGrid[1].First();
                    tableFinish = intGrid.Length;

                    files = intGrid.Select((arr, i) =>
                        arr.Skip(1).Select((time, j) => new FileInfo() {
                            start = i.ToString(),
                            end = (j + 1).ToString(),
                            time = time
                        })
                    ).SelectMany(arr => arr.Where(f => f.time < 2500 && f.start != f.end)).ToArray();
                } catch { throw new Exception("Invalid table input"); }
            }

            // Update things, as input parsed successfully
            canListOrSolve = true;
        }
        catch (Exception e) {
            files = null;
            if (interrupt) {
                Error.Throw(e.Message);
                canListOrSolve = false;
                Console.ReadLine();
            }
        }

        RefreshActionAbility();
    }

    // for making a table about lobby files for example
    public static void FetchFileTimes()
    {
        bool startFileFound = false;
        bool finishFileFound = false;
        bool menuingInAllFiles = true;

        foreach (var file in files) {
            Panel.WriteCol(file.start, Color.LawnGreen);
            Panel.WriteCol(" to ", Color.White);
            Panel.WriteCol(file.end, Color.Violet);
            Panel.WriteCol(": ", Color.White);
            Panel.WriteCol($"Stamp: {Misc.AsSeconds(file.time)}({file.time})", Color.SkyBlue);
            if (Settings.timestampAt1X && !Settings.useTableInput) {
                Panel.WriteCol(", ", Color.White);
                Panel.WriteCol($"Filewise: {GetFilewise()}\n", Color.FromArgb(251, 255, 138));
            }
            else
                Console.WriteLine();

            string GetFilewise()
            {
                int fileTime = file.time;
                string fileContent = File.ReadAllText(file.directory);

                if (Settings.lobbyStart.Contains(file.start)) {
                    fileTime += Settings.restartPenalty;
                    startFileFound = true;
                }

                if (Misc.hasMenuing.IsMatch(fileContent)) {
                    fileTime += Misc.shorterMenu.IsMatch(fileContent) ? 66 : 97;
                    if (Settings.lobbyFinish.Contains(file.end))
                        finishFileFound = true;
                }
                else if (!Settings.lobbyFinish.Contains(file.end)) {
                    fileTime += 97;
                    menuingInAllFiles = false;
                }
                else
                    finishFileFound = true;

                return $"{Misc.AsSeconds(fileTime)}({fileTime})";
            }
        }

        if (!menuingInAllFiles) Panel.WriteCol("Not all files had menuing in them. Filewise times may be inaccurate.\n", Color.White);
        if (!startFileFound) Panel.WriteCol("The name of the start did not appear. Filewise times from it are inaccurate.\n", Color.White);
        if (!finishFileFound) Panel.WriteCol("The name of the finish did not appear. Filewise times to it may be inaccurate.\n", Color.White);
    }

    public static void SolveLobby()
    {
        // put the name of every distinct place to an array
        string[] places;
        try {
            places = files.Select(f => f.start)
                .Concat(files.Select(f => f.end))
                .Distinct().ToArray();
        } catch { return; }
        // get amount of places minus the start, for checking pathfind trail length
        int placeCount = Settings.useTableInput ? tableFinish : places.Length - 1;

        // parse `files` into a more efficient pathfinding data structure; check PlaceInfo structure.
        PlaceInfo[] infos = places.Select(
            p => new PlaceInfo() {
                    name = p,
                    targets = new(from i in files where i.start == p
                                  select new KeyValuePair<int, int>(
                                      Array.IndexOf(places, i.end),
                                      files.First(f => f.start == p && f.end == i.end).time))
            }).ToArray();

        int spawn  = Settings.useTableInput ? 0 : Array.IndexOf(places, Settings.lobbyStart[0]);
        int finish = Settings.useTableInput ? tableFinish : Array.IndexOf(places, Settings.lobbyFinish[0]);
        if (spawn == -1 || finish == -1) {
            Error.Throw("Either the spawn or finish name could not be found in file name parsings.");
            return;
        }

        var solutions = new List<Tuple<int[], int>>();

        int iterations = 0;
        int restartCount = 0;
        bool infRestarts = Settings.maxRestarts < 0;
        int restartPenalty = Settings.useTableInput ? tableRestartPenalty : Settings.restartPenalty;


        // do the brute force pathfinding
        List<int> trail = new(placeCount + 1);
        bool GoneEverywhere() => trail.Count(p => p != spawn) == placeCount;
        PathFind(spawn);


        solutions = solutions.OrderByDescending(s => s.Item2).ToList();
        // normal console output
        if (!Settings.useTxtOutput)
            foreach (var sol in solutions) {
                var split = ParseSolution(sol).Split(':');
                Panel.WriteCol(split[0] + ':', Color.LightSkyBlue);
                Panel.WriteCol(split[1] + '\n', Color.White);
            }
        // output into txt file directory
        else {
            File.WriteAllText(
                Settings.outputFile,
                string.Join('\n', solutions.Select(sol => ParseSolution(sol))));
        }
        Console.WriteLine(solutions.Count + " solutions");
        Console.WriteLine($"Pathfind function called {iterations} times.");

        string ParseSolution(Tuple<int[], int> sol) => $"{Misc.AsSeconds(sol.Item2)}({sol.Item2}) with: {string.Join(", ", sol.Item1.Skip(1).Select(p => (p == spawn ? "RESTART" : places[p])))}";

        // give debug advice if no solutions
        if (solutions.Count == 0) {
            Panel.WriteCol("You got zero solutions. Try running ", Color.Yellow);
            Panel.WriteCol("List file times", Color.LightPink);
            Panel.WriteCol("to see if file names were parsed correctly.\n", Color.Yellow);
            Panel.SetColor(Color.White);
        }
        // mini menu for printing out an approx draft of finished lobby file
        else if (!Settings.useTxtOutput && !Settings.useTableInput) {
            Panel.WriteCol("\n\n --- Post Solution Actions ---\n", Color.LimeGreen);
            DraftingMenu(false);

            while (true) {
                Panel.inputRow = Console.CursorTop;
                string raw = Console.ReadLine();
                var input = Misc.ParseInput(raw.ToLower());

                if (input.Item1 == "" || !"deq".Contains(input.Item1[0])) {
                    Panel.CursorBack();
                    Console.Write(new string(' ', raw.Length));
                    Panel.CursorBack();
                    continue;
                }
                else if (input.Item1[0] == 'd') {
                    int.TryParse(input.Item2, out int index);
                    int[] trl = solutions[^(index + 1)].Item1;

                    Panel.SetColor(Color.Khaki);

                    Console.WriteLine("\n\n#INSERT TAS START\n");
                    for (int i = 1; i < trl.Length; i++) {
                        if (trl[i] == spawn) {
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
                    Panel.WriteCol("\n\nunsafe\n  20\n   1,J\n  10\n#NoCredits\n  20\n   1,J\n  46\n\n\n", Color.Khaki);
                    Panel.SetColor(Color.PeachPuff);
                    Console.WriteLine("It is assumed that your lobby files end with 1,X.");
                    Console.WriteLine("Some maps are missing the credits page, making them faster to enter so\nwhen this is the case, use this suffix: \"Read,EnterLevel,NoCredits\".");
                    Console.WriteLine("The last line of this file might have to be 47 frames instead. I don't know when though.");
                }
                else
                    return;
                DraftingMenu();
            }
        }
        Console.ReadLine();

        // end of solve solutions function


        void PathFind(int pos)
        {
            trail.Add(pos);

            iterations++;
            
            // if finished, process the solution
            if (pos == finish && GoneEverywhere()) {
                var skip1 = trail.Skip(1).ToArray();
                int time = skip1.Select((e, i) => e == spawn ? restartPenalty : infos[trail[i]].targets[e]).Sum();
                if (!Settings.distinctTimes || solutions.All(s => time != s.Item2))
                    solutions.Add(new(trail.ToArray(), time));
            }

            // branch out to all current position connections that havent been visited
            bool mustRestart = true;
            foreach (int target in infos[pos].targets.Keys.Where(k => !trail.Contains(k))) {
                mustRestart = false;
                PathFind(target);
            }
            
            // restarts
            if (CanRestart()) {
                restartCount++;
                PathFind(spawn);
                restartCount--;
            }

            trail.RemoveAt(trail.Count - 1);

            bool CanRestart()
            {
                if (pos == spawn || restartCount == Settings.maxRestarts)
                    return false;
                if (Settings.requiredRestarts)
                    return mustRestart;
                else
                    return true;
            }
        }
    }

    private static void DraftingMenu(bool separatorLine = true)
    {
        if (separatorLine)
            Panel.WriteCol("-----------------------------------", Color.Lime);
        Panel.WriteCol("\nd: ", Color.Yellow);
        Panel.WriteCol("Print approximated draft for final main TAS file\n", Color.LightSteelBlue);
        Panel.WriteCol("d {0 index}: ", Color.Yellow);
        Panel.WriteCol("Pick which route to print the draft of\n", Color.LightSteelBlue);
        Panel.WriteCol("e: ", Color.Yellow);
        Panel.WriteCol("Print and explain the \"ExitLevel\" inputs if you need but dont have them\n", Color.LightSeaGreen);
        Panel.WriteCol("q: ", Color.Yellow);
        Panel.WriteCol("Quit to main control panel\n", Color.Orange);
        Panel.WriteCol("Input: ", Color.White);
    }
}

static class Error
{
    public static Color col = Color.FromArgb(255, 61, 61);
    public static void Throw(string msg)
    {
        Panel.SetColor(col);
        Console.WriteLine("Error: " + msg);
        Panel.SetColor(Color.White);
    }
}
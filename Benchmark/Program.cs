using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboRouter.Bench;

// Benchmark for the router. Runs the lobby tables of a test data file through AlgRunner, times each run
// and compares the console output against a stored reference, so it can be confirmed that optimizations dont change the results.
// References are gitignored and can be created with --save-ref before optimizing.
// References are kept in a "reference" folder next to the data file.
// Running the same command without --save-ref compares against references automatically.
//
// usage: RoboRouter.Bench <datasets|all> [options]
//        RoboRouter.Bench generate <out file> <min places> <max places> <step> <seeds> [<moves per place>,...]
//   datasets           comma separated names (Beginner,Intermediate,...) or "all"
//   --mode <m>         newconn (Test New Connections on, default) | solve
//   --top <n>          Number of Solutions (default 10)
//   --no-progress      Print Detailed Progress off
//   --connections <s>  New connections input, e.g. "13-18, 0-20" (default: test all)
//   --runs <n>         repeat each run n times and log the min and median (default 1)
//   --save-ref         store the output as the reference instead of comparing against it
//   --data <path>      test data file (default: Benchmark/test-data.txt)
//   --cpu <n>          pin the process to specific cpu for more stable timings
//   --threads <n>      number of threads for testing new connections (default 1)
//
// More test data can be generated with the LobbyGenerator, e.g.:
//   RoboRouter.Bench generate Benchmark/generated/test-data-generated.txt 20 64 4 3
//   RoboRouter.Bench generate Benchmark/generated/test-data-dense.txt 16 64 4 3 6,8,10,12,16
public static class Program
{
    static readonly Regex ansi = new(@"\x1b\[[0-9;]*m");
    static readonly string[] volatilePrefixes = {
        "Routing took", "Pathfind function calls", "Branches cut", "Full solutions calculated", "Total time"
    };

    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "generate")
            return LobbyGenerator.Run(args[1..]);

        string root = FindRoot();
        string dataPath = Path.Combine(root, "Benchmark", "test-data.txt");
        string datasetArg = "all";
        string mode = "newconn";
        int top = 10, runs = 1;
        bool saveRef = false, noProgress = false;
        string connections = "Format: 13-18, 0-20";
        int cpu = -1;
        int threads = 1;

        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

        var positional = new List<string>();
        for (int i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--mode": mode = args[++i]; break;
                case "--top": top = int.Parse(args[++i]); break;
                case "--runs": runs = int.Parse(args[++i]); break;
                case "--no-progress": noProgress = true; break;
                case "--connections": connections = args[++i]; break;
                case "--save-ref": saveRef = true; break;
                case "--data": dataPath = args[++i]; break;
                case "--cpu": cpu = int.Parse(args[++i]); break;
                case "--threads": threads = int.Parse(args[++i]); break;
                default: positional.Add(args[i]); break;
            }
        }
        if (positional.Count > 0)
            datasetArg = positional[0];

        if (cpu >= 0)
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1L << cpu);

        var datasets = ParseDatasets(File.ReadAllText(dataPath));
        var selected = datasetArg == "all"
            ? datasets.Keys.ToList()
            : datasetArg.Split(',').Select(s => s.Trim()).ToList();

        string refDir = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(dataPath))!, "reference");
        string outDir = Path.Combine(root, "Benchmark", "out");
        Directory.CreateDirectory(refDir);
        Directory.CreateDirectory(outDir);

        var realOut = Console.Out;
        bool allMatch = true;

        foreach (var name in selected) {
            if (!datasets.TryGetValue(name, out var table)) {
                realOut.WriteLine($"Unknown dataset '{name}'. Known: {string.Join(", ", datasets.Keys)}");
                return 2;
            }

            var times = new List<double>();
            string output = "";
            for (int run = 0; run < runs; run++) {
                var settings = new Settings {
                    UseTableInput = true,
                    TableInput = table,
                    newConnectionsMode = mode == "newconn",
                    NewConnectionsInput = connections,
                    topNSolutions = top,
                    LogResults = false,
                    PrintDetailedProgress = !noProgress,
                };
                var files = TableParser.Parse(settings.TableInput, out int tableRestartPenalty);

                Solver.TotalIterations = 0;
                var capture = new StringWriter();
                Console.SetOut(capture);
                var sw = Stopwatch.StartNew();
                try {
                    var runner = new AlgRunner(files, settings, "", "", tableRestartPenalty) { threads = threads };
                    if (settings.newConnectionsMode) runner.FindNewConnections(); else runner.SolveLobby();
                }
                finally {
                    sw.Stop();
                    Console.SetOut(realOut);
                }
                times.Add(sw.Elapsed.TotalMilliseconds);
                output = Normalize(capture.ToString());
            }

            string key = $"{name}-{mode}-t{top}";
            if (!connections.StartsWith("Format:"))
                key += "-c" + Hash(connections)[..8];
            if (noProgress)
                key += "-np";
            string outPath = Path.Combine(outDir, key + ".txt");
            File.WriteAllText(outPath, output);
            string refPath = Path.Combine(refDir, key + ".txt");
            string status;
            if (saveRef) {
                File.WriteAllText(refPath, output);
                status = "saved as reference";
            }
            else if (File.Exists(refPath)) {
                bool match = Normalize(File.ReadAllText(refPath)) == output;
                allMatch &= match;
                status = match ? "MATCH" : $"MISMATCH (diff {Path.GetRelativePath(root, refPath)} {Path.GetRelativePath(root, outPath)})";
            }
            else {
                status = "no reference (should be created by running with --save-ref)";
            }

            times.Sort();
            string timing = runs == 1
                ? $"{times[0],10:F1} ms"
                : $"min {times[0],10:F1} ms  median {times[times.Count / 2],10:F1} ms";
            string threadInfo = mode != "newconn" ? "" : $" x{threads,-2}";
            realOut.WriteLine($"{name,-13} {mode,-7}{threadInfo} {timing}  nodes {Solver.TotalIterations,13:N0}  hash {Hash(output)}  {status}");
        }

        return allMatch ? 0 : 1;
    }

    static string Normalize(string raw)
    {
        var lines = ansi.Replace(raw, "").Replace("\r", "").Split('\n')
            .Where(l => !volatilePrefixes.Any(p => l.StartsWith(p)));
        return string.Join("\n", lines).TrimEnd();
    }

    static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))[..12];

    static Dictionary<string, string> ParseDatasets(string text)
    {
        var result = new Dictionary<string, string>();
        var matches = Regex.Matches(text, @"^Table Input (\w+):\s*$", RegexOptions.Multiline);
        for (int i = 0; i < matches.Count; i++) {
            int bodyStart = matches[i].Index + matches[i].Length;
            int bodyEnd = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            result[matches[i].Groups[1].Value] = "\n" + text[bodyStart..bodyEnd].Trim();
        }
        return result;
    }

    static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "RoboRouter.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}

using System.Text;

namespace RoboRouter.Bench;

// Generate fake lobby data for the benchmarks
public static class LobbyGenerator
{
    const int NoConnection = 60000;

    public static string Generate(int places, int seed, double? targetDegree = null)
    {
        var rng = new Random(seed * 1000 + places);
        int finish = places - 1;
        int last = places - 2;
        var time = new int[places, places];
        for (int i = 0; i < places; i++)
            for (int j = 0; j < places; j++)
                time[i, j] = i == j ? 0 : NoConnection;

        int Travel(double scale = 1) => Math.Clamp((int)(scale * 180 * Math.Exp(0.35 * Normal(rng))), 40, 560);

        void Link(int a, int b, bool bothWays, double scale = 1)
        {
            int t = Travel(scale);
            if (bothWays || rng.Next(2) == 0) {
                time[a, b] = Math.Min(time[a, b], t);
                if (bothWays)
                    time[b, a] = Math.Min(time[b, a], (int)(t * (0.7 + 0.7 * rng.NextDouble())));
            }
            else {
                time[b, a] = Math.Min(time[b, a], t);
            }
        }

        for (int i = 1; i <= last; i++) {
            if (i + 1 <= last) {
                Link(i, i + 1, rng.NextDouble() < 0.65);
                time[i, i + 1] = Math.Min(time[i, i + 1], Travel());
            }
            if (i + 2 <= last && rng.NextDouble() < 0.65)
                Link(i, i + 2, rng.NextDouble() < 0.65);
            if (i + 3 <= last && rng.NextDouble() < 0.5)
                Link(i, i + 3, rng.NextDouble() < 0.65);
            if (rng.NextDouble() < 0.8) {
                int far = i + 4 + rng.Next(Math.Max(1, places / 2 - 4));
                if (far <= last)
                    Link(i, far, rng.NextDouble() < 0.65, 1.2);
            }
        }

        for (int i = 1; i <= last; i++) {
            for (int tries = 0; tries < 20 && (OutDegree(time, i, places) < 2 || InDegree(time, i, places) < 2); tries++) {
                int other = Math.Clamp(i + (rng.Next(2) == 0 ? -1 : 1) * (1 + rng.Next(3)), 1, last);
                if (other != i)
                    Link(i, other, true);
            }
        }

        double density = 1;
        if (targetDegree is double target) {
            density = target / 4.4;
            int moves = 0;
            for (int i = 1; i <= last; i++)
                moves += OutDegree(time, i, places) - (time[i, finish] < NoConnection ? 1 : 0);
            for (int tries = 0; moves < target * last && tries < 100 * places * places; tries++) {
                int i = 1 + rng.Next(last);
                int j = rng.NextDouble() < 0.6
                    ? i + (rng.Next(2) == 0 ? -1 : 1) * (2 + rng.Next(5))
                    : 1 + rng.Next(last);
                if (j < 1 || j > last || j == i)
                    continue;
                bool had = time[i, j] < NoConnection, hadBack = time[j, i] < NoConnection;
                Link(i, j, rng.NextDouble() < 0.65);
                moves += (time[i, j] < NoConnection && !had ? 1 : 0) + (time[j, i] < NoConnection && !hadBack ? 1 : 0);
            }
        }

        int startLinks = Math.Min(last, (int)Math.Round(density * Math.Clamp(Math.Round(last / 5.0), 4, 10)));
        foreach (int place in new[] { 1 }.Concat(Enumerable.Range(2, last - 1).OrderBy(_ => rng.Next())).Take(startLinks))
            time[0, place] = 70 + rng.Next(230);

        int finishLinks = Math.Min(last, (int)Math.Round(density * Math.Clamp(Math.Round(last / 4.0), 5, 10)));
        int finishBase = 700 + rng.Next(300);
        foreach (int place in new[] { last }.Concat(Enumerable.Range(1, last - 1).OrderBy(_ => rng.Next())).Take(finishLinks))
            time[place, finish] = finishBase + rng.Next(400);

        var sb = new StringBuilder();
        for (int i = 0; i < places; i++) {
            var row = new int[places];
            for (int j = 0; j < places; j++)
                row[j] = time[i, j];
            row[0] = i == 0 || i == finish ? 0 : 190;
            if (i == finish)
                for (int j = 1; j < finish; j++)
                    row[j] = NoConnection;
            sb.Append('[').Append(string.Join(",", row)).Append("]\n");
        }
        return sb.ToString();
    }

    static int OutDegree(int[,] time, int i, int places)
    {
        int count = 0;
        for (int j = 1; j < places; j++)
            if (j != i && time[i, j] < NoConnection)
                count++;
        return count;
    }

    static int InDegree(int[,] time, int i, int places)
    {
        int count = 0;
        for (int j = 1; j < places - 1; j++)
            if (j != i && time[j, i] < NoConnection)
                count++;
        return count;
    }

    static double Normal(Random rng) =>
        Math.Sqrt(-2 * Math.Log(1 - rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());

    // usage: RoboRouter.Bench generate <out file> <min places> <max places> <step> <seeds> [degrees, e.g. 6,8,10]
    public static int Run(string[] args)
    {
        string outPath = args[0];
        int min = int.Parse(args[1]), max = int.Parse(args[2]), step = int.Parse(args[3]), seeds = int.Parse(args[4]);
        var degrees = args.Length > 5 ? args[5].Split(',').Select(int.Parse).ToArray() : null;
        var sb = new StringBuilder();
        sb.Append("Generated lobby tables.\n");
        sb.Append($"Regenerate: RoboRouter.Bench generate <this file> {string.Join(" ", args[1..])}\n");
        if (degrees == null) {
            sb.Append("Names: Gen<places>s<seed>\n\n");
            for (int places = min; places <= max; places += step) {
                for (int seed = 1; seed <= seeds; seed++) {
                    sb.Append($"Table Input Gen{places:D2}s{seed}:\n\n");
                    sb.Append(Generate(places, seed)).Append('\n');
                }
            }
        }
        else {
            sb.Append("Denser lobbies. Names: D<average moves out of a place>N<places>s<seed>\n\n");
            foreach (int degree in degrees) {
                for (int places = min; places <= max; places += step) {
                    for (int seed = 1; seed <= seeds; seed++) {
                        sb.Append($"Table Input D{degree:D2}N{places:D2}s{seed}:\n\n");
                        sb.Append(Generate(places, seed, degree)).Append('\n');
                    }
                }
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllText(outPath, sb.ToString().Replace("\n", "\r\n"));
        Console.WriteLine($"Wrote {outPath}");
        return 0;
    }
}

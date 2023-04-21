using System.Text.RegularExpressions;

namespace RoboRouter;

public static class Misc
{
    private static Regex formatTime = new(@"(\d+)[\.,]?(\d{0,3})\d*");
    public static string AsSeconds(int frames) => formatTime.Replace((frames * 0.017).ToString(), m => m.Result("$1.") + m.Groups[2].Value.PadRight(3, '0'));

    public static (string, string) ParseInput(string input)
    {
        var match = Regex.Match(input, @"^([^\s]*)(?:\s?(.*))");
        return new(match.Groups[1].Value, match.Groups[2].Value);
    }

    public static Regex hasMenuing  = new Regex(@"\d+,X(\s+\d+\s+\d+,[JO]\s|\s*$)");
    public static Regex shorterMenu = new Regex(@"1,X(?:,[JO])?(?>([^#](#.*)?)*?[JO](?:,[JO])?)(?!([^#]*(?>#.*)?)*[JO])");

    public static Regex fileNameFromDir = new Regex(@"(?<=([\\/]))[^\\/]+.[^\\/]+(?=[\\/]?$)");
}

public struct FileInfo
{
    public string directory;
    public string start;
    public string end;
    public int time;
}

public struct PlaceInfo
{
    public string name;

    public int[] targets;
    public int[] targeters;

    public int[] times;

    public int FramesTo(int place) => times[Array.IndexOf(targets, place)];
}

public struct Solution {
    public int[] path;
    public int time;

    public Solution(int[] _path, int _time) {
        path = _path;
        time = _time;
    }
}

public struct ConnectionResult {
    public string connectionName;
    public string parsedSol;
    public int timeNeeded;

    public ConnectionResult(string _connectionName, string _parsedSol, int _timeNeeded) {
        connectionName = _connectionName;
        parsedSol = _parsedSol;
        timeNeeded = _timeNeeded;
    }
}
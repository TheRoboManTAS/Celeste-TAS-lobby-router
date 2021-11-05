using static System.Console;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing;
using static AppSettings;

static class Panel
{
    public static void Print()
    {
        Clear();

        if (Settings == new settings())
            WriteCol("\nType 'h' and enter for help\n\n", Color.Orange);

        ForegroundColor = ConsoleColor.DarkGray;
        WriteLine("####################################");
        ForegroundColor = ConsoleColor.White;

        foreach (var line in settingTexts) {
            if (line.dependency == null || line.dependency()) {
                WriteCol(line.chr, Color.Yellow);
                WriteCol(line.name, line.col);
                if (line.valueGetter != null) {
                    string settingVal = line.valueGetter();
                    if (settingVal == null)
                        WriteCol("undefined", Error.col);
                    else
                        WriteCol(settingVal, Color.LightSteelBlue);
                }
                WriteLine();
            }
        }

        ForegroundColor = ConsoleColor.DarkGray;
        WriteLine("####################################");
        ForegroundColor = ConsoleColor.White;

        Write("Input: ");
        inputRow = CursorTop;
    }

    private static Line[] settingTexts = {
        new("h:   ", "How to use, and info | ", null,                                       null,                          Color.PaleGreen),
        new("b:        ", "Use table input | ", () => Settings.useTableInput.ToString(),    null,                          Color.BurlyWood),
        new("e:            ", "Input table | ", () => Settings.inputTable,                  () => Settings.useTableInput,  Color.BurlyWood),
        new("d:       ", "Source directory | ", () => Settings.directory,                   () => !Settings.useTableInput, Color.White),
        new("n:        ", "Name separators | ", () => $"\"{Settings.nameSeparators}\"",     () => !Settings.useTableInput, Color.White),
        new("s:      ", "Name of the start | ", () => ConnectOptions(Settings.lobbyStart),  () => !Settings.useTableInput, Color.White),
        new("f:     ", "Name of the finish | ", () => ConnectOptions(Settings.lobbyFinish), () => !Settings.useTableInput, Color.White),
        new("t:       ", "Timestamp at 1,X | ", () => Settings.timestampAt1X.ToString(),    () => !Settings.useTableInput, Color.Aqua),
        new("u:    ", "Output to text file | ", () => Settings.useTxtOutput.ToString(),     null,                          Color.DarkTurquoise),
        new("x:       ", "Output directory | ", () => Settings.outputFile,                  () => Settings.useTxtOutput,   Color.DarkTurquoise),
        new("i:  ", "Distinct finish times | ", () => Settings.distinctTimes.ToString(),    null,                          Color.Aquamarine),
        new("r:        ", "Restart penalty | ", () => Settings.restartPenalty.ToString(),   () => !Settings.useTableInput, Color.Orchid),
        new("o: ", "Only required restarts | ", () => Settings.requiredRestarts.ToString(), null,                          Color.Orchid),
        new("c:    ", "Restart count limit | ", () => Settings.maxRestarts.ToString(),      null,                          Color.Orchid),
        new("a:       ", "Solve all routes | ", null,                                       null,                          Color.LightPink),
        new("l:        ", "List file times | ", null,                                       () => !Settings.useTableInput, Color.LightPink)
    };

    private static string ConnectOptions(string[] input) => input == null ? null : string.Join(" || ", input);

    class Line
    {
        public string chr;
        public string name;
        public Func<string> valueGetter;
        public Func<bool> dependency;
        public Color col;

        public Line(string chr, string name, Func<string> valueGetter, Func<bool> dependency, Color col)
        {
            this.chr = chr;
            this.name = name;
            this.valueGetter = valueGetter;
            this.dependency = dependency;
            this.col = col;
        }
    }

    public static int inputRow = 11;
    public static void CursorBack() => SetCursorPosition(7, inputRow);

    public static Action<Color> SetColor = (Color col) => Write("\x1b[38;2;" + col.R + ";" + col.G + ";" + col.B + "m");

    public static void WriteCol(string s, Color col)
    {
        SetColor(col);
        Write(s);
    }
}

static class Misc
{
    private static Regex formatTime = new(@"(\d+)[\.,]?(\d{0,3})\d*");
    public static string AsSeconds(int frames) => formatTime.Replace((frames * 0.017).ToString(), m => m.Result("$1.") + m.Groups[2].Value.PadRight(3, '0'));
    
    public static Tuple<string, string> ParseInput(string input)
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
    public string name; // name of the starting place on the input files
    public Dictionary<int, int> targets; // the key is the index of the target location and value is the time it takes to it.
}

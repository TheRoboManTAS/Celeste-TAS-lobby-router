using static System.Console;
using static System.Drawing.Color;
using System.Xml.Serialization;
using System.IO;
using static AppSettings;

using System.Runtime.InteropServices;

class Program
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleMode(System.IntPtr handle, int mode);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetConsoleMode(System.IntPtr handle, out int mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern System.IntPtr GetStdHandle(int handle);

    static void Main()
    {
        Title = "Lobby Routing Tool";

        var handle = GetStdHandle(-11);
        GetConsoleMode(handle, out int mode);
        if (!SetConsoleMode(handle, mode | 0x4))
            Panel.SetColor = col => { };

        string configName = "config.xml";
        if (!File.Exists(configName)) {
            Error.Throw(configName + " could not be found in current context. create in current file? y/n: ");
            while (true) {
                string input = ReadLine().Trim().ToLower();
                if (input == "")     continue;
                if (input[0] == 'y') break;
                if (input[0] == 'n') return;
            }
        }
        config = File.Open(configName, FileMode.OpenOrCreate);

        // attempt loading saved settings
        try { Settings = (settings)ser.Deserialize(config); }
        catch {
            Settings = new();
            SaveSettings();
        }

        AppActions.ReparseInput(false);

        Panel.Print(true);

        while (true) {
            string input = ReadLine();
            int len = input.Length;
            input = input.TrimStart();

            if (string.IsNullOrEmpty(input)) { Panel.CursorBack(); continue; }
            char ltr1 = char.ToLower(input[0]);

            if (!"hbednsftuxiroc".Contains(ltr1)) {
                if ("al".Contains(ltr1)) {
                    if (AppActions.canListOrSolve) {
                        Clear();
                        if (ltr1 == 'a') {
                            WriteLine("Loading...");
                            AppActions.SolveLobby();
                        }
                        else {
                            AppActions.FetchFileTimes();
                            ReadLine();
                        }
                    }
                    else {
                        Error.Throw("Settings are not properly set for this action.");
                        ReadLine();
                    }
                    Panel.Print();
                }
                else {
                    Panel.CursorBack();
                    Write(new string(' ', len));
                    Panel.CursorBack();
                    continue;
                }
            }
            else {
                var rest = System.Text.RegularExpressions.Regex.Match(input, @"(?<= ).*").Value;
                var eval = Misc.ParseInput(input);
                if (eval.Item2 == "") {
                    Panel.SetColor(DarkSeaGreen);
                    WriteLine(ltr1 switch {
                        'h' => "To get info about a setting or run an action, write only the letter of it.\nTo set the value of a setting, write the letter of it, then the value to set it to.\nFor booleans, write the value as y/n or t/f.\nMade by TheRoboMan.",
                        'b' => "tntfalle.\nTo use the 2D array-like string from a premade table instead of parsing files.\n:) You know what you're doing if you wanna use this.",
                        'e' => "The table of times to route, where Y (starting from up to down) is the starting location\nand X is the target location. Each line of the table except the last one must\nend with a comma, else it thinks youve finished writing the table.\nIndex 0 is the beginning and the last index is the finish.",
                        'd' => "The root directory of the folder containing all and\nonly the lobby TAS files that you want to route.\nDirectory can be copied by clicking the top directory bar in resource manager.",
                        'n' => "For parsing which part of each lobby file's name is its beginning and ending point,\nspecifying characters that separate parts of the file name is necessary.",
                        's' => "The name of the start location, which is where you go when you enter or restart the lobby.\nMultiple shown places means an error because none of them had files that go to them.",
                        'f' => "The name of the finish location, which is the place you have to go after visiting every map.\nMultiple shown places means an error because none of them had files that go to them.",
                        't' => "Used only for the list file times function.\nTells if the time on the file's timestamp is\nthe time on the frame of 1,X or at the end of the menuing",
                        'u' => "Sometimes there are too many solutions to fit in the console buffer.\nYou can use this option if you need access to all solutions.\nNote: The file gets emptied before writing in it.",
                        'x' => "The directory of the text file that will get emptied then written in.\nCan be copied by clicking the top directory bar in resource manager.",
                        'i' => "With this on, solutions with identical finish times to other solutions in the list do not count.",
                        'r' => "When you enter or restart the lobby, the file timer is running\nwhile chapter timer isn't during the start animation. This tells that amount of time.\nExample: Wakeup animation -> 190",
                        'o' => "When this is enabled, restarts can occur only if you run\ninto a dead end and must restart in order to progress.",
                        'c' => "How many restarts the route can have at most.\nAny negative number means there's no limit",
                        _ => ""
                    });

                    Panel.SetColor(White);
                    Write("Input: ");
                    Panel.inputRow = CursorTop;
                }
                else {
                    switch (ltr1) {
                        case 'b':
                            BoolEdit(ref Settings.useTableInput);
                            AppActions.ReparseInput(false);
                            break;
                        case 'e':
                            Clear();
                            Settings.inputTable = "";
                            rest = rest.Trim();
                            WriteLine(rest);
                            while (true) {
                                if (rest == "")
                                    break;
                                Settings.inputTable += '\n' + rest;
                                if (rest[^1] != ',')
                                    break;
                                rest = ReadLine().Trim();
                            }
                            if (Settings.useTableInput)
                                AppActions.ReparseInput();
                            break;
                        case 'd':
                            Settings.directory = rest;
                            if (!Settings.useTableInput)
                                AppActions.ReparseInput();
                            break;
                        case 'n':
                            Settings.nameSeparators = rest;
                            AppActions.ReparseInput(false);
                            break;
                        case 's':
                            Settings.lobbyStart = new string[] { rest }; break;
                        case 'f':
                            Settings.lobbyFinish = new string[] { rest }; break;
                        case 't':
                            BoolEdit(ref Settings.timestampAt1X); break;
                        case 'u':
                            BoolEdit(ref Settings.useTxtOutput);
                            AppActions.RefreshActionAbility();
                            break;
                        case 'x':
                            Settings.outputFile = rest;
                            AppActions.RefreshActionAbility();
                            break;
                        case 'i':
                            BoolEdit(ref Settings.distinctTimes); break;
                        case 'r':
                            int.TryParse(rest, out Settings.restartPenalty); break;
                        case 'o':
                            BoolEdit(ref Settings.requiredRestarts); break;
                        case 'c':
                            int.TryParse(rest, out Settings.maxRestarts); break;
                    }
                    Panel.Print();

                    SaveSettings();

                    void BoolEdit(ref bool edit)
                    {
                        char ltr = char.ToLower(rest.TrimStart()[0]);
                        if ("yt".Contains(ltr)) edit = true;
                        else if ("nf".Contains(ltr)) edit = false;
                    }
                }
            }
        }
    }
}

static class AppSettings
{
    public static settings Settings;
    public static FileStream config;
    public static XmlSerializer ser = new(typeof(settings));
    public static void SaveSettings()
    {
        config.SetLength(0);
        ser.Serialize(config, Settings);
    }
}
public class settings
{
    public bool useTableInput = false;
    public string inputTable = null;
    public string directory = null;
    public string nameSeparators = "-";
    public string[] lobbyStart = null;
    public string[] lobbyFinish = null;
    public bool timestampAt1X = true;
    public bool useTxtOutput = false;
    public string outputFile = null;
    public bool distinctTimes = true;
    public int restartPenalty = 190;
    public bool requiredRestarts = true;
    public int maxRestarts = -1;
}
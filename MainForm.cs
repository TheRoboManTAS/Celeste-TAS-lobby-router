using System.Text.RegularExpressions;
using System.Xml.Serialization;
using static System.Drawing.Color;
using System.Diagnostics;

namespace RoboRouter;

public partial class MainForm : Form
{
    public Settings settings;

    public FileInfo[] files;

    public string startFile;
    public string finishFile;
    public PlaceState start;
    public PlaceState finish;

    public int tableRestartPenalty;

    public bool mouseOnDirBox;

    private AlgRunner? oldRunner;
    public MainForm()
    {
        InitializeComponent();
        maxRestarts = new NumMenuItem(maxRestartCountToolStripMenuItem, -1, 1000, 0);

        LoadSettings();
        ParseFiles(false);
        UpdateStartAndEnd(false);
        UpdateAccess();

        Output.Initialize();
        Select();
        txt_directory.MouseEnter += (_, _) => mouseOnDirBox = true;
        txt_directory.MouseLeave += (_, _) => mouseOnDirBox = false;
        txt_directory.Enter += (_, _) => {
            if (mouseOnDirBox) {
                using var dialog = new FolderBrowserDialog();
                var res = dialog.ShowDialog();
                if (res == DialogResult.OK) {
                    if (txt_directory.Text != dialog.SelectedPath) {
                        txt_directory.Text = settings.Directory = dialog.SelectedPath;
                        btn_refresh_Click(this, new EventArgs());
                    }
                    txt_directory.Select(9999, 0);
                }
            }
        };

        FormClosed += (_, _) => SaveSettings();

        txt_finishName.Leave += (_, _) => txt_finishName.Text = txt_finishName.Text.Trim();

        if (!settings.KnowsHelpTxt) {
            new Thread(() => {
                bool toggle = false;
                var yellow = FromArgb(240, 255, 50);
                var normal = FromArgb(245, 245, 245);
                try {
                    while (true) {
                        if (settings.KnowsHelpTxt) {
                            Invoke(() => { helpToolStripMenuItem.BackColor = normal; });
                            return;
                        }
                        Invoke(() => { helpToolStripMenuItem.BackColor = toggle ? normal : yellow; });
                        toggle = !toggle;
                        Thread.Sleep(500);
                    }
                }
                catch { }
            }).Start();
        }
    }

    private bool ParseFiles(bool feedback)
    {
        if (!Directory.Exists(settings.Directory)) {
            if (feedback)
                Output.PrintError("The specified directory does not exist.\n");
            return false;
        }

        if (string.IsNullOrEmpty(settings.NameSeparators))
            settings.NameSeparators = txt_nameSeparators.Text = "-";
        string sep = Regex.Escape(settings.NameSeparators);

        Regex parseName = new(@$"(?!.*\\)([^{sep}]+)[{sep}]+([^{sep}]+)\.tas");
        Regex regularTimeStamp = new(@"^#(?> *)(?!$)(?:(\d+) *: *)?0*(\d+[.,]\d+)? *(?:\(? *(\d+) *f? *\)?)? *($|\r|\n)", RegexOptions.Multiline);
        Regex chapterTimeStamp = new(@"^ChapterTime: *\d+:\d+\.\d+\((\d+)\) *($|\r|\n)", RegexOptions.Multiline);

        List<FileInfo> fileLs = new();

        foreach (var dir in Directory.GetFiles(settings.Directory, "*.tas", SearchOption.AllDirectories)) {
            var nameMatch = parseName.Match(dir);

            if (!nameMatch.Success) {
                if (feedback)
                    Output.PrintError($"Could not read the file name {dir}\n");
                return false;
            }

            int time;
            string inputs = File.ReadAllText(dir);
            if (inputs.Contains("\nChapterTime:")) {
                var stamps = chapterTimeStamp.Matches(inputs);
                if (stamps.Count == 0) {
                    if (feedback)
                        Output.PrintError($"Could not read ChapterTime command in {dir}\n");
                    return false;
                }
                var stamp = stamps[^1];
                time = int.Parse(stamp.Groups[1].Value);
            }
            else {
                var stamps = regularTimeStamp.Matches(inputs);
                if (stamps.Count == 0) {
                    if (feedback)
                        Output.PrintError($"Could not find time stamp in {dir}\n");
                    return false;
                }
                var stamp = stamps[^1];
                time = (int)Math.Round(float.Parse(stamp.Groups[2].Value.Replace(',', '.')) / 0.017f);
            }

            fileLs.Add(new() {
                directory = dir,
                start = nameMatch.Groups[1].Value,
                end = nameMatch.Groups[2].Value,
                time = time
            });
        }

        files = fileLs.ToArray();

        if (feedback)
            Console.WriteLine("Successfully parsed files.");

        settings.KnowsHelpTxt = true;
        return true;
    }

    private bool ParseTable()
    {
        try {
            Regex getNum = new(@"\d+");
            // skip first row because input table always starts with '\n' so first elt will be empty
            // skip last row because we dont account for going from finish to elsewhere
            var intGrid = Regex.Matches(settings.TableInput, @".*\d.*").SkipLast(1)
            .Select(ln => getNum.Matches(ln.Value)
                .Select(m => int.Parse(m.Value)).ToArray()).ToArray();

            tableRestartPenalty = intGrid[1][0];

            files = intGrid.Select((arr, i) =>
                arr.Skip(1).Select((time, j) => new FileInfo() {
                    start = i.ToString(),
                    end = (j + 1).ToString(),
                    time = time
                })
            ).SelectMany(arr => arr.Where(f => f.time < 2500 && f.start != f.end)).ToArray();

            return true;
        }
        catch {
            Output.PrintError("Failed to parse table input.");
            return false;
        }
    }

    public void UpdateStartAndEnd(bool feedback)
    {
        if (files is null) return;
        var starts = files.Where(f => files.All(i => i.end != f.start)).Select(f => f.start).Distinct().ToArray();
        var finishes = files.Where(f => files.All(i => i.start != f.end)).Select(f => f.end).Distinct().ToArray();

        if (starts.Length != 1) {
            if (starts.Length > 1) {
                start = PlaceState.TooMany;
                startFile = string.Join(" | ", starts);
            }
            else {
                startFile = null;
                start = PlaceState.NotFound;
            }
        }
        else {
            startFile = starts[0];
            start = PlaceState.Good;
        }
        txt_startName.Text = startFile;

        if (finishes.Length != 1) {
            if (finishes.Length == 0)
                finish = PlaceState.NotFound;
            else {
                if (string.IsNullOrEmpty(finishFile))
                    finish = PlaceState.TooMany;
                else
                    finish = finishes.Any(fn => finishFile == fn)
                     ? PlaceState.Good : PlaceState.Invalid;
            }
            if (feedback || string.IsNullOrEmpty(finishFile))
                finishFile = finishes.Length == 0 ? null : string.Join(" | ", finishes);
            else if (feedback && finish != PlaceState.Good)
                finishFile = null;
        }
        else {
            finishFile = finishes[0];
            finish = PlaceState.Good;
        }

        txt_finishName.Text = finishFile;
    }

    public bool StartEndFeedback()
    {
        Output.PrintError(start switch {
            PlaceState.NotFound => "A start location could not be found, meaning every map had a file leading to them.\n",
            PlaceState.TooMany => "A start location could not be chosen, meaning there were multiple places where no file ended at them.\nAre the name separators correct?\n",
            _ => ""
        });
        Output.PrintError(finish switch {
            PlaceState.NotFound => "A finish location could not be found, meaning there was no place without a file starting from it.\n",
            PlaceState.Invalid => "The finish name either doesn't exist or has files starting from it, making it invalid.\n",
            PlaceState.TooMany => "There are too many places that might be the finish.\nTry choosing it manually in the finish name text box or correcting the name separators.\n",
            _ => ""
        });
        return (start | finish) == PlaceState.Good;
    }

    public void UpdateAccess()
    {
        foreach (var ctrl in new Control[] { btn_fetchTimes, txt_nameSeparators, txt_startName, txt_finishName, btn_refresh, num_restartPenalty, txt_directory })
            ctrl.Enabled = !cbx_useTableInput.Checked;
        txt_tableInput.Enabled = cbx_useTableInput.Checked;
    }

    NumMenuItem maxRestarts;

    SettingsManager manager;

    public void LoadSettings()
    {
        settings = new Settings();
        try {
            using var file = new FileStream("settings.xml", FileMode.OpenOrCreate);
            settings = new XmlSerializer(typeof(Settings)).Deserialize(file) as Settings ?? settings;
        }
        catch { }

        manager = new SettingsManager(settings, this);
        manager.Load();
    }

    public void SaveSettings()
    {
        manager.Save();

        finishFile = txt_finishName.Text;
        startFile = txt_startName.Text;

        // actually save stuff in the settings file
        using var file = new FileStream("settings.xml", FileMode.OpenOrCreate);
        file.SetLength(0);
        new XmlSerializer(typeof(Settings)).Serialize(file, settings);
    }

    private void btn_route_Click(object sender, EventArgs e)
    {
        SaveSettings();
        Output.ClearAndShow(true);

        bool ready = true;

        if (settings.UseTableInput)
            ready = ParseTable();
        else {
            ready &= ParseFiles(true);
            UpdateStartAndEnd(false);
            ready &= StartEndFeedback();
        }

        if (ready) {
            if (oldRunner != null)
                oldRunner.stillRunning = false;
            oldRunner = new AlgRunner(this);
            Console.WriteLine("Loading...");
            new Thread(oldRunner.SolveLobby).Start();
        }
    }

    private void btn_fetchTimes_Click(object sender, EventArgs e)
    {
        SaveSettings();
        Output.ClearAndShow(true);
        if (ParseFiles(true))
            new AlgRunner(this).FetchFileTimes();
    }

    private void btn_refresh_Click(object sender, EventArgs e)
    {
        finishFile = null;
        SaveSettings();
        Output.ClearAndShow(true);
        ParseFiles(true);
        UpdateStartAndEnd(true);
    }

    private void onlyRequiredRestartsToolStripMenuItem_Click(object sender, EventArgs e) => onlyRequiredRestartsToolStripMenuItem.Checked ^= true;
    private void distinctResultEndTimesToolStripMenuItem_Click(object sender, EventArgs e) => distinctResultEndTimesToolStripMenuItem.Checked ^= true;
    private void logResultsToTextFilesToolStripMenuItem_Click(object sender, EventArgs e) => logResultsToTextFilesToolStripMenuItem.Checked ^= true;
    private void disableResultSortingToolStripMenuItem_Click(object sender, EventArgs e) => disableResultSortingToolStripMenuItem.Checked ^= true;

    private void cbx_useTableInput_CheckedChanged(object sender, EventArgs e) => UpdateAccess();

    private void helpToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!File.Exists(helpFile)) {
            var file = File.Create(helpFile);
            file.Write(System.Text.Encoding.UTF8.GetBytes(helpText));
            file.Close();
        }
        Process.Start("notepad.exe", helpFile);
        settings.KnowsHelpTxt = true;
    }

    const string helpFile = "help.txt";
    const string helpText = @"
How to get started:

1: Click the 'Lobby TAS Folder' text box and select the folder that contains all of the lobby files you want to route.
   Note that every TAS file in the folder has to be a lobby file, and that it will find files in
   branching directories of the given folder as well.

2: Adjust the 'Restart Penalty' value if you need to. It's the same as how long it takes to gain control when you enter the lobby.

3: Try clicking the 'Run Router' button. It might work already, but if it doesn't,
   it will give you feedback in what needs to be fixed.

4: You can disable 'Only Dead End Restarts' in the 'Restarts' dropdown menu to route the lobby more thoroughly.
   This allows many many more mostly useless routes and might make very big lobbies take long to compute.



Table input is the secondary way to use the pathfinding algorithm.
It's only useful if you're already using something like a spreadsheet to keep track of lobby TASing progress.
In a routing table, the Y axis is the start location and the X axis is the target location.
A table looks something like this:

[0,259,60000,60000,60000,60000,60000,60000,60000,60000,116,147,60000,128,4],
[190,0,218,60000,60000,60000,60000,60000,60000,60000,60000,60000,60000,236,162],
[190,177,0,241,60000,60000,60000,60000,60000,60000,60000,60000,60000,60000,60000],
[190,60000,285,0,197,60000,60000,60000,60000,60000,60000,60000,60000,60000,60000],
[190,60000,60000,249,0,232,60000,60000,60000,60000,60000,60000,60000,60000,60000],
[190,60000,60000,60000,222,0,247,60000,60000,60000,60000,60000,206,60000,60000],
[190,60000,60000,60000,60000,345,0,233,60000,60000,94,60000,133,60000,60000],
[190,60000,60000,60000,60000,60000,213,0,164,185,152,60000,60000,60000,60000],
[190,60000,60000,60000,60000,60000,60000,257,0,282,336,60000,60000,60000,60000],
[190,60000,60000,60000,60000,60000,60000,296,329,0,317,337,60000,60000,60000],
[190,60000,60000,60000,60000,60000,196,215,262,218,0,139,193,60000,153],
[190,60000,60000,60000,60000,60000,60000,60000,60000,199,119,0,60000,60000,167],
[190,60000,60000,60000,60000,312,128,60000,60000,60000,169,60000,0,60000,60000],
[190,269,60000,60000,60000,60000,60000,60000,60000,60000,181,207,60000,0,66],
[0,60000,60000,60000,60000,60000,60000,60000,60000,60000,60000,60000,60000,60000,0]

Exact formatting of the table does not matter. It only looks for the separate numbers per line.
";
}

public enum PlaceState
{
    Good = 0,
    NotFound = 1,
    TooMany = 2,
    Invalid = 3
}

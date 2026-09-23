namespace RoboRouter;

public class Settings
{
    [Setting(Input = "txt_directory.Text")]
    public string? Directory;

    [Setting(Input = "txt_nameSeparators.Text")]
    public string NameSeparators = "-";
    [Setting(Input = "txt_finishName.Text")]
    public string? FinishName;

    [Setting(Input = "cbx_useTableInput.Checked")]
    public bool UseTableInput = false;
    [Setting(Input = "txt_tableInput.Text")]
    public string? TableInput;

    [Setting(Input = "cbx_newConnectionsMode.Checked")]
    public bool newConnectionsMode = false;
    [Setting(Input = "txt_newConnectionsInput.Text")]
    public string NewConnectionsInput = "Format: 13-18, 0-20";
    [Setting(Input = "cbx_printDetailedProgress.Checked")]
    public bool PrintDetailedProgress = false;

    [Setting(Input = "num_restartPenalty.Value")]
    public int restartPenalty = 190;
    [Setting(Input = "topNSolutions.Value")]
    public int topNSolutions = 100;

    [Setting(Input = "multithreadingToolStripMenuItem.Checked")]
    public bool Multithreading = false;
    [Setting(Input = "threadCount.Value")]
    public int ThreadCount = DefaultThreadCount(Environment.ProcessorCount);

    public static int DefaultThreadCount(int logicalCores) =>
        logicalCores >= 12 ? logicalCores / 2 :
        logicalCores >= 4 ? logicalCores * 3 / 4 :
        Math.Max(1, logicalCores);

    // [Setting(Input = "distinctResultEndTimesToolStripMenuItem.Checked")]
    // public bool DistinctTimings = true;
    [Setting(Input = "logResultsToTextFilesToolStripMenuItem.Checked")]
    public bool LogResults = false;
    //[Setting(Input = "disableResultSortingToolStripMenuItem.Checked")]
    //public bool DisableSorting = false;

    public bool KnowsHelpTxt = false;
}

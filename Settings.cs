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

    [Setting(Input = "num_restartPenalty.Value")]
    public int restartPenalty = 190;
    [Setting(Input = "onlyRequiredRestartsToolStripMenuItem.Checked")]
    public bool RequiredRestarts = true;
    [Setting(Input = "maxRestarts.Value")]
    public int maxRestarts = -1;

    // [Setting(Input = "distinctResultEndTimesToolStripMenuItem.Checked")]
    // public bool DistinctTimings = true;
    [Setting(Input = "logResultsToTextFilesToolStripMenuItem.Checked")]
    public bool LogResults = false;
    //[Setting(Input = "disableResultSortingToolStripMenuItem.Checked")]
    //public bool DisableSorting = false;

    public bool KnowsHelpTxt = false;
}

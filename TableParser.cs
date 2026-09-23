using System.Text.RegularExpressions;

namespace RoboRouter;

public static class TableParser
{
    public static FileInfo[] Parse(string? tableInput, out int restartPenalty)
    {
        Regex getNum = new(@"\d+");
        // skip first row because input table always starts with '\n' so first elt will be empty
        // skip last row because we dont account for going from finish to elsewhere
        var intGrid = Regex.Matches(tableInput ?? "", @".*\d.*").SkipLast(1)
        .Select(ln => getNum.Matches(ln.Value)
            .Select(m => int.Parse(m.Value)).ToArray()).ToArray();

        restartPenalty = intGrid[1][0];

        return intGrid.Select((arr, i) =>
            arr.Skip(1).Select((time, j) => new FileInfo() {
                start = i.ToString(),
                end = (j + 1).ToString(),
                time = time
            })
        ).SelectMany(arr => arr.Where(f => f.time < 10000 && f.start != f.end)).ToArray();
    }
}

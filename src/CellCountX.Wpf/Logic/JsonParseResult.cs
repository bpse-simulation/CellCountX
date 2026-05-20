namespace CellCountX.Wpf.Logic;

public class JsonParseResult
{
    public bool IsError { get; set; }
    public string ErrorMessage { get; set; } = "";
    public int Count { get; set; }
    public bool GpuUsed { get; set; }
    public int FilteredCount { get; set; }
    public bool NonadherentsRemoved { get; set; }
    public string MaskPath { get; set; } = "";
}

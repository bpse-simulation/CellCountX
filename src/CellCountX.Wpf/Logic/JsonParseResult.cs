namespace CellCountX.Wpf.Logic;

public class JsonParseResult
{
    public bool IsError { get; set; }
    public string ErrorMessage { get; set; } = "";
    public int Count { get; set; }
    public bool GpuUsed { get; set; }
}

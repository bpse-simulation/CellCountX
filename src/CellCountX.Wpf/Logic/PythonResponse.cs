namespace CellCountX.Wpf.Logic;

public class PythonResponse
{
    public bool IsError { get; set; }
    public string ErrorMessage { get; set; } = "";
    public string RawOutput { get; set; } = "";
}

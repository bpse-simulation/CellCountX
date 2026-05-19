namespace CellCountX.Wpf.Model;

public class CellResult
{
    public string FileName { get; set; } = "";
    public int CellCount { get; set; }
    public int FilteredCellCount { get; set; }
    public string MaskPath { get; set; } = "";
}

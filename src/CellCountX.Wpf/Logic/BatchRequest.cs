namespace CellCountX.Wpf.Logic;

public class BatchRequest
{
    public string InputFolder { get; set; } = "";
    public string OutputFolder { get; set; } = "";
    public bool UseGpu { get; set; }

    // MainViewModel で GPU/CPU に応じて自動設定され、
    // 詳細設定で上書きされる可能性がある
    public int TimeoutSeconds { get; set; }

    // 死細胞除去パラメータ（UI → Python へ渡す）
    public bool RemoveDeadCells { get; set; }

    public int MinArea { get; set; } = 50;
    public double MaxCircularity { get; set; } = 0.85;
    public double MaxIntensity { get; set; } = 0.6;
    public double MinVariance { get; set; } = 50;
}

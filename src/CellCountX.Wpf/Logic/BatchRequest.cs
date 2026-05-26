namespace CellCountX.Wpf.Logic;

public class BatchRequest
{
    public string InputFolder { get; set; } = "";
    public string OutputFolder { get; set; } = "";
    public bool UseGpu { get; set; }

    // MainViewModel で GPU/CPU に応じて自動設定され、
    // 詳細設定で上書きされる可能性がある
    public int TimeoutSeconds { get; set; }

    // 非接着細胞除去パラメータ（UI → Python へ渡す）
    public bool RemoveNonAdherents { get; set; }

    public int MinArea { get; set; } = 0;
    public double MaxCircularity { get; set; } = 1.0;
    public double MaxIntensity { get; set; } = 255.0;
    public double MinVariance { get; set; } = 0;
}

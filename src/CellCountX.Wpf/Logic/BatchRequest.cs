namespace CellCountX.Wpf.Logic;

public class BatchRequest
{
    public string InputFolder { get; set; } = "";
    public string OutputFolder { get; set; } = "";
    public bool UseGpu { get; set; }

    // MainViewModel で GPU/CPU に応じて自動設定され、
    // 詳細設定で上書きされる可能性がある
    public int TimeoutSeconds { get; set; }

    // RF フィルタ使用の有無（UI → Python へ渡す）
    public bool UseRfFilter { get; set; }
}

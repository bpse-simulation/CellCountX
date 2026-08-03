namespace CellCountX.Wpf.Logic;

public class BatchRequest
{
    public string InputFolder { get; set; } = "";
    public string OutputFolder { get; set; } = "";
    public bool UseGpu { get; set; }

    // MainViewModel で GPU/CPU に応じて自動設定され、
    // 詳細設定で上書きされる可能性がある
    public int TimeoutSeconds { get; set; }

    // Cellpose モデルパス（.npy）
    // 空欄または null の場合は server.py 側で "cpsam_v2" を使用
    public string? CellposeModelPath { get; set; }

    // 境界フィルタリングの有効化
    public bool UseEdgeFilter { get; set; }
    public bool UseEdgeTop { get; set; }
    public bool UseEdgeBottom { get; set; }
    public bool UseEdgeLeft { get; set; }
    public bool UseEdgeRight { get; set; }
    public int EdgeMargin { get; set; }
}

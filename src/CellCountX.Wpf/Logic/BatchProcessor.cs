using System.IO;
using System.Text.Json;
using CellCountX.Wpf.Model;

namespace CellCountX.Wpf.Logic;

public class BatchProcessor(PythonClient python)
{
    private readonly PythonClient _python = python;
    private readonly JsonParser _jsonParser = new();
    private readonly CsvExporter _csvExporter = new();

    public event Action<string>? Log;
    public event Action<double>? Progress;
    public event Action<List<CellResult>>? Completed;

    public async Task StartAsync(BatchRequest req, CancellationToken token)
    {
        // バッチ処理開始時に使用モデルをログ出力
        Log?.Invoke($"使用モデル: {(string.IsNullOrEmpty(req.CellposeModelPath) ? "cpsam_v2" : req.CellposeModelPath)}");

        // 全角パスチェック
        if (req.InputFolder.Any(c => c > 127))
        {
            Log?.Invoke("入力フォルダのパスに全角文字が含まれています。処理を中断します。");
            Completed?.Invoke([]);
            return;
        }

        var files = Directory
            .EnumerateFiles(req.InputFolder)
            .Where(PathValidator.IsImageFile)
            .ToList();

        int total = files.Count;
        int count = 0;

        Log?.Invoke($"バッチ処理開始（{total} 件）");

        var results = new List<CellResult>();

        foreach (var file in files)
        {
            if (token.IsCancellationRequested)
            {
                Log?.Invoke("キャンセルされました。");
                break;
            }

            Progress?.Invoke((double)count / total * 100);

            if (PathValidator.ContainsNonAscii(file))
            {
                Log?.Invoke($"ファイル名に全角文字が含まれています： {Path.GetFileName(file)}");
                count++;
                continue;
            }

            Log?.Invoke($"処理中: {Path.GetFileName(file)}");

            // Python に渡す JSON
            var payload = new
            {
                path = file,
                gpu = req.UseGpu,
                output = req.OutputFolder,

                // Cellpose モデル
                custom_model = req.CellposeModelPath,

                // 境界細胞除去（4方向 + マージン）
                use_edge_filter = req.UseEdgeFilter,
                use_edge_top = req.UseEdgeTop,
                use_edge_bottom = req.UseEdgeBottom,
                use_edge_left = req.UseEdgeLeft,
                use_edge_right = req.UseEdgeRight,
                edge_margin = req.EdgeMargin
            };

            string json = JsonSerializer.Serialize(payload);

            try
            {
                // ★ タイムアウトは PythonClient が一元管理
                var py = await _python.RunAsync(json, req.TimeoutSeconds, token);

                if (py.IsError)
                {
                    Log?.Invoke($"Python エラー: {py.ErrorMessage}");
                    count++;
                    continue;
                }

                var parsed = _jsonParser.Parse(py.RawOutput);

                if (parsed.IsError)
                {
                    Log?.Invoke($"JSON パースエラー: {parsed.ErrorMessage}");
                    count++;
                    continue;
                }

                // ★ CellResult に非接着細胞除去後の細胞数を追加
                results.Add(new CellResult
                {
                    FileName = Path.GetFileName(file),
                    CellCount = parsed.Count,
                    FilteredCellCount = parsed.FilteredCount,
                });

                Log?.Invoke($"結果: {parsed.Count} → フィルタ後: {parsed.FilteredCount}");
            }
            catch (Exception ex)
            {
                // ★ PythonClient 内部例外（Kill 失敗など）もここで拾う
                Log?.Invoke($"例外: {ex.Message}");
            }

            count++;
        }

        Progress?.Invoke(100);

        // ★ CSV に FilteredCellCount を含めて保存
        string savedPath = _csvExporter.Save(results, req.OutputFolder);
        // cells.csv 以外なら別名保存されたということ

        if (!savedPath.EndsWith("cells.csv"))
        {
            Log?.Invoke($"CSV は Excel によってロックされていたため、別名で保存しました: {savedPath}");
        }

        Log?.Invoke("バッチ処理完了");

        Completed?.Invoke(results);
    }
}

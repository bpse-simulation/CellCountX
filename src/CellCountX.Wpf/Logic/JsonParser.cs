using System.Text.Json;

namespace CellCountX.Wpf.Logic;

public class JsonParser
{
    public JsonParseResult Parse(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            return Error("Python の出力が空です");

        // 最後の JSON 行を抽出
        var jsonLine = rawOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Reverse()
            .FirstOrDefault(l =>
            {
                var t = l.Trim();
                return t.StartsWith("{") && t.EndsWith("}");
            });

        if (jsonLine == null)
            return Error("Python の出力に JSON が見つかりません");

        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            var root = doc.RootElement;

            // Python 側のエラー
            if (root.TryGetProperty("error", out var err))
                return Error(err.GetString() ?? "Unknown error");

            // Cellpose の元の細胞数
            int count = root.GetProperty("count").GetInt32();

            // 死細胞除去後の細胞数（なければ count と同じ）
            int filteredCount = root.TryGetProperty("filtered_count", out var fc)
                ? fc.GetInt32()
                : count;

            // GPU 使用
            bool gpu = root.TryGetProperty("gpu_used", out var g) && g.GetBoolean();

            // 死細胞除去が実行されたか
            bool deadRemoved = root.TryGetProperty("dead_removed", out var dr) && dr.GetBoolean();

            // マスクパス
            string maskPath = root.TryGetProperty("mask_path", out var mp)
                ? mp.GetString() ?? ""
                : "";

            return new JsonParseResult
            {
                IsError = false,
                Count = count,
                FilteredCount = filteredCount,
                GpuUsed = gpu,
                DeadRemoved = deadRemoved,
                MaskPath = maskPath
            };
        }
        catch (Exception ex)
        {
            return Error($"JSON パースエラー: {ex.Message}");
        }
    }

    private JsonParseResult Error(string msg)
        => new JsonParseResult { IsError = true, ErrorMessage = msg };
}

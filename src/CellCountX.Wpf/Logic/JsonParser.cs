using System.Text.Json;

namespace CellCountX.Wpf.Logic;

public class JsonParser
{
    public JsonParseResult Parse(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            return Error("Python の出力が空です");

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
            // ★ PythonResult は使わない
            var doc = JsonDocument.Parse(jsonLine);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var err))
                return Error(err.GetString() ?? "Unknown error");

            int count = root.GetProperty("count").GetInt32();
            bool gpu = root.TryGetProperty("gpu_used", out var g) && g.GetBoolean();

            return new JsonParseResult
            {
                IsError = false,
                Count = count,
                GpuUsed = gpu
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

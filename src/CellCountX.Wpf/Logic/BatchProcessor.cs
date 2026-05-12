using System.IO;
using System.Text.Json;
using CellCountX.Wpf.Model;

namespace CellCountX.Wpf.Logic;

public class BatchProcessor
{
    private readonly PythonClient _python;
    private readonly JsonParser _jsonParser;
    private readonly CsvExporter _csvExporter;

    public event Action<string>? Log;
    public event Action<double>? Progress;
    public event Action<List<CellResult>>? Completed;

    public BatchProcessor(PythonClient python)
    {
        _python = python;
        _jsonParser = new JsonParser();
        _csvExporter = new CsvExporter();
    }

    public async Task StartAsync(BatchRequest req, CancellationToken token)
    {
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

            var payload = new
            {
                path = file,
                gpu = req.UseGpu,
                output = req.OutputFolder
            };

            string json = JsonSerializer.Serialize(payload);

            try
            {
                // PythonClient を使う
                var py = await _python.RunAsync(json, req.TimeoutSeconds);

                if (py.IsError)
                {
                    Log?.Invoke($"Python エラー: {py.ErrorMessage}");
                    count++;
                    continue;
                }

                // JSON パース
                var parsed = _jsonParser.Parse(py.RawOutput);

                if (parsed.IsError)
                {
                    Log?.Invoke($"JSON パースエラー: {parsed.ErrorMessage}");
                    count++;
                    continue;
                }

                results.Add(new CellResult
                {
                    FileName = Path.GetFileName(file),
                    CellCount = parsed.Count
                });

                Log?.Invoke($"結果: {parsed.Count}");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"例外: {ex.Message}");
            }

            count++;
        }

        Progress?.Invoke(100);

        _csvExporter.Save(results, req.OutputFolder);
        Log?.Invoke("バッチ処理完了");

        Completed?.Invoke(results);
    }
}

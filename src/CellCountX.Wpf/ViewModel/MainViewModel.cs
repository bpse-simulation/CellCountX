using CellCountX.Wpf.Model;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace CellCountX.Wpf.ViewModel;

public class MainViewModel : INotifyPropertyChanged
{
    // ---------------------------------------------------------
    // プロパティ
    // ---------------------------------------------------------
    private string _inputFolder = "";
    public string InputFolder
    {
        get => _inputFolder;
        set { _inputFolder = value; OnPropertyChanged(nameof(InputFolder)); }
    }

    private bool _useGpu;
    public bool UseGpu
    {
        get => _useGpu;
        set { _useGpu = value; OnPropertyChanged(nameof(UseGpu)); }
    }

    private string _logText = "";
    public string LogText
    {
        get => _logText;
        set { _logText = value; OnPropertyChanged(nameof(LogText)); }
    }

    private double _progressValue;
    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(nameof(ProgressValue)); }
    }

    private int _timeoutSeconds = 60;
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set { _timeoutSeconds = value; OnPropertyChanged(nameof(TimeoutSeconds)); }
    }

    private string _outputFolder = "";
    public string OutputFolder
    {
        get => _outputFolder;
        set { _outputFolder = value; OnPropertyChanged(nameof(OutputFolder)); }
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            _isRunning = value;
            OnPropertyChanged(nameof(IsRunning));
            StartBatchCommand.RaiseCanExecuteChanged();
            CancelBatchCommand.RaiseCanExecuteChanged();
        }
    }

    // ---------------------------------------------------------
    // コマンド
    // ---------------------------------------------------------
    public ICommand BrowseFolderCommand { get; }
    public ICommand BrowseOutputFolderCommand { get; }
    public RelayCommand StartBatchCommand { get; }
    public RelayCommand CancelBatchCommand { get; }

    private CancellationTokenSource? _cts;

    // Python サーバー（1回実行版）
    private readonly PythonServer _pythonServer = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
        BrowseOutputFolderCommand = new RelayCommand(_ => BrowseOutputFolder());
        StartBatchCommand = new RelayCommand(async _ => await StartBatchAsync(), _ => !IsRunning);
        CancelBatchCommand = new RelayCommand(_ => CancelBatch(), _ => IsRunning);
    }

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ---------------------------------------------------------
    // フォルダ選択
    // ---------------------------------------------------------
    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            var folderPath = dialog.FolderName;

            if (ContainsNonAscii(folderPath))
            {
                MessageBox.Show(
                    "選択したフォルダのパスに全角文字が含まれています。\n" +
                    "CellPose が正しく動作しないため、英数字のみのパスに移動してください。\n\n" +
                    $"選択されたパス：\n{folderPath}",
                    "パスエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            InputFolder = folderPath;
            AppendLog($"画像フォルダ選択: {InputFolder}");
        }
    }

    private void BrowseOutputFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            OutputFolder = dialog.FolderName;
            AppendLog($"出力フォルダ選択: {OutputFolder}");
        }
    }

    private static bool ContainsNonAscii(string path)
        => path.Any(c => c > 127);

    // ---------------------------------------------------------
    // バッチ処理開始
    // ---------------------------------------------------------
    private async Task StartBatchAsync()
    {
        if (!Directory.Exists(InputFolder))
        {
            AppendLog("フォルダが存在しません。");
            return;
        }

        IsRunning = true;
        _cts = new CancellationTokenSource();

        var files = Directory
            .EnumerateFiles(InputFolder)
            .Where(IsImageFile)
            .ToList();

        int total = files.Count;
        int count = 0;

        AppendLog($"バッチ処理開始（{total} 件）");

        var results = new List<CellResult>();

        foreach (var file in files)
        {
            UpdateProgress(count, total);

            if (_cts.IsCancellationRequested)
            {
                AppendLog("キャンセルされました。");
                break;
            }

            if (ContainsNonAscii(file))
            {
                AppendLog($"ファイル名に全角文字が含まれています： {Path.GetFileName(file)}");
                count++;
                continue;
            }

            AppendLog($"処理中: {Path.GetFileName(file)}");

            var payload = new
            {
                path = file,
                gpu = UseGpu,
                output = OutputFolder
            };

            string json = JsonSerializer.Serialize(payload);

            try
            {
                string response = await Task.Run(() => _pythonServer.RunOnce(json, TimeoutSeconds));

                string? jsonLine = ExtractJsonLine(response);

                if (jsonLine == null)
                {
                    AppendLog("Python の出力に JSON が見つかりませんでした。");
                    AppendLog(response);
                    continue;
                }

                var obj = JsonSerializer.Deserialize<PythonResult>(jsonLine);
                if (obj == null)
                {
                    AppendLog($"結果の解析に失敗: {response}");
                    continue;
                }

                results.Add(new CellResult
                {
                    FileName = Path.GetFileName(file),
                    CellCount = obj.CellCount
                });

                AppendLog($"結果: {jsonLine}");
            }
            catch (Exception ex)
            {
                AppendLog($"エラー: {ex.Message}");
            }

            count++;
        }

        IsRunning = false;
        ProgressValue = 100;

        SaveResultsToCsv(results, OutputFolder);
        AppendLog("バッチ処理完了");
    }

    // ---------------------------------------------------------
    // JSON 行抽出
    // ---------------------------------------------------------
    private static string? ExtractJsonLine(string response)
    {
        var lines = response.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        return lines
            .Reverse()
            .FirstOrDefault(l =>
            {
                var t = l.Trim();
                return t.StartsWith('{') && t.EndsWith('}');
            });
    }

    // ---------------------------------------------------------
    // キャンセル
    // ---------------------------------------------------------
    private void CancelBatch()
    {
        ProgressValue = 0;
        _cts?.Cancel();
        _pythonServer.RequestCancel();
    }

    // ---------------------------------------------------------
    // ログ追加
    // ---------------------------------------------------------
    private void AppendLog(string message)
    {
        LogText += $"{DateTime.Now:HH:mm:ss}  {message}\n";
    }

    private static bool IsImageFile(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext is ".png" or ".bmp" or ".tif" or ".tiff" or ".jpg" or ".jpeg";
    }

    private void UpdateProgress(int count, int total)
    {
        ProgressValue = total == 0 ? 0 : (double)count / total * 100;
    }

    // ---------------------------------------------------------
    // CSV 保存
    // ---------------------------------------------------------
    private static void SaveResultsToCsv(List<CellResult> results, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        var csvPath = Path.Combine(outputFolder, "cells.csv");

        var lines = new List<string> { "FileName,CellCount" };

        foreach (var r in results)
        {
            string file = EscapeCsv(r.FileName);
            string count = r.CellCount.ToString();
            lines.Add($"{file},{count}");
        }

        File.WriteAllLines(csvPath, lines, Encoding.UTF8);
    }

    private static string EscapeCsv(string s)
        => (s.Contains(',') || s.Contains(' ')) ? $"\"{s}\"" : s;
}

public class PythonResult
{
    [JsonPropertyName("count")]
    public int CellCount { get; set; }

    [JsonPropertyName("gpu_used")]
    public bool GpuUsed { get; set; }
}

public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    private readonly Action<object?> _execute = execute;
    private readonly Func<object?, bool>? _canExecute = canExecute;

    public bool CanExecute(object? parameter)
        => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter)
        => _execute(parameter);

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

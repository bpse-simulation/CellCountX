using CellCountX.Wpf.Logic;
using CellCountX.Wpf.Model;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace CellCountX.Wpf.ViewModel;

public class MainViewModel : INotifyPropertyChanged
{
    // ---------------------------------------------------------
    // プロパティ（UI 状態）
    // ---------------------------------------------------------
    private string _inputFolder = "";
    public string InputFolder
    {
        get => _inputFolder;
        set { _inputFolder = value; OnPropertyChanged(nameof(InputFolder)); }
    }

    private string _outputFolder = "";
    public string OutputFolder
    {
        get => _outputFolder;
        set { _outputFolder = value; OnPropertyChanged(nameof(OutputFolder)); }
    }

    private bool _useGpu;
    public bool UseGpu
    {
        get => _useGpu;
        set { _useGpu = value; OnPropertyChanged(nameof(UseGpu)); }
    }

    private int _timeoutSeconds = 600;
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set { _timeoutSeconds = value; OnPropertyChanged(nameof(TimeoutSeconds)); }
    }

    private double _progressValue;
    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(nameof(ProgressValue)); }
    }

    private string _logText = "";
    public string LogText
    {
        get => _logText;
        set { _logText = value; OnPropertyChanged(nameof(LogText)); }
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

    // ---------------------------------------------------------
    // 内部
    // ---------------------------------------------------------
    private readonly BatchProcessor _processor;
    private CancellationTokenSource? _cts;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        // PythonServer → PythonClient → BatchProcessor の構成
        var pythonServer = new PythonServer();
        var pythonClient = new PythonClient(pythonServer);
        _processor = new BatchProcessor(pythonClient);

        // イベント購読
        _processor.Log += msg => AppendLog(msg);
        _processor.Progress += v => ProgressValue = v;
        _processor.Completed += _ =>
        {
            IsRunning = false;
            AppendLog("処理が完了しました。");
        };

        // コマンド
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
            InputFolder = dialog.FolderName;
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

        var req = new BatchRequest
        {
            InputFolder = InputFolder,
            OutputFolder = OutputFolder,
            UseGpu = UseGpu,
            TimeoutSeconds = TimeoutSeconds
        };

        await _processor.StartAsync(req, _cts.Token);

        // 中断・エラー時もここで UI を復帰
        IsRunning = false;
    }

    // ---------------------------------------------------------
    // キャンセル
    // ---------------------------------------------------------
    private void CancelBatch()
    {
        _cts?.Cancel();
        AppendLog("キャンセル要求を送信しました。");
    }

    // ---------------------------------------------------------
    // ログ追加
    // ---------------------------------------------------------
    private void AppendLog(string message)
    {
        LogText += $"{DateTime.Now:HH:mm:ss}  {message}\n";
    }

    // ---------------------------------------------------------
    // 起動ログ
    // ---------------------------------------------------------
    public void AppendStartupLog()
    {
        AppendLog("CellCountX 起動");
    }
}

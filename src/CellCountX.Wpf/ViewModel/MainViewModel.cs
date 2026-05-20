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
        set
        {
            if (_useGpu == value) return;

            var oldUseGpu = _useGpu;
            _useGpu = value;
            OnPropertyChanged(nameof(UseGpu));

            // 以前が自動タイムアウト値だった場合のみ、新しい自動値に追従させる
            if (TimeoutSeconds == GetAutoTimeout(oldUseGpu) || TimeoutSeconds <= 0)
            {
                TimeoutSeconds = GetAutoTimeout(_useGpu);
            }
        }
    }

    private int _timeoutSeconds = 60;
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            if (_timeoutSeconds == value) return;
            _timeoutSeconds = value;
            OnPropertyChanged(nameof(TimeoutSeconds));
        }
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
    // 非接着細胞除去パラメータ（UI から調整可能）
    // ---------------------------------------------------------
    private bool _removenonadherents;
    public bool RemoveNonAdherents
    {
        get => _removenonadherents;
        set
        {
            if (_removenonadherents == value) return;
            _removenonadherents = value;
            OnPropertyChanged(nameof(RemoveNonAdherents));

            Properties.Settings.Default.RemoveNonAdherents = value;
            Properties.Settings.Default.Save();
        }
    }

    private int _minArea = 0;
    public int MinArea
    {
        get => _minArea;
        set
        {
            if (_minArea == value) return;
            _minArea = value;
            OnPropertyChanged(nameof(MinArea));

            Properties.Settings.Default.MinArea = value;
            Properties.Settings.Default.Save();
        }
    }

    private double _maxCircularity = 1;
    public double MaxCircularity
    {
        get => _maxCircularity;
        set
        {
            if (_maxCircularity == value) return;
            _maxCircularity = value;
            OnPropertyChanged(nameof(MaxCircularity));

            Properties.Settings.Default.MaxCircularity = value;
            Properties.Settings.Default.Save();
        }
    }

    private double _maxIntensity = 255;
    public double MaxIntensity
    {
        get => _maxIntensity;
        set
        {
            if (_maxIntensity == value) return;
            _maxIntensity = value;
            OnPropertyChanged(nameof(MaxIntensity));

            Properties.Settings.Default.MaxIntensity = value;
            Properties.Settings.Default.Save();
        }
    }

    private double _minVariance = 0;
    public double MinVariance
    {
        get => _minVariance;
        set
        {
            if (_minVariance == value) return;
            _minVariance = value;
            OnPropertyChanged(nameof(MinVariance));

            Properties.Settings.Default.MinVariance = value;
            Properties.Settings.Default.Save();
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
        // 設定読み込み
        UseGpu = Properties.Settings.Default.UseGpu;

        // 詳細設定で TimeoutSeconds が設定されていればそれを使う。0 以下なら自動値。
        var savedTimeout = Properties.Settings.Default.TimeoutSeconds;
        TimeoutSeconds = savedTimeout > 0 ? savedTimeout : GetAutoTimeout(UseGpu);

        // 非接着細胞除去の ON/OFF を復元
        RemoveNonAdherents = Properties.Settings.Default.RemoveNonAdherents;

        // パラメータも復元
        MinArea = Properties.Settings.Default.MinArea;
        MaxCircularity = Properties.Settings.Default.MaxCircularity;
        MaxIntensity = Properties.Settings.Default.MaxIntensity;
        MinVariance = Properties.Settings.Default.MinVariance;

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

    internal int GetAutoTimeout(bool useGpu)
        => useGpu ? 300 : 900; // GPU: 5分, CPU: 15分

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

        // 非接着細胞除去パラメータを含めて Python に渡す
        var req = new BatchRequest
        {
            InputFolder = InputFolder,
            OutputFolder = OutputFolder,
            UseGpu = UseGpu,
            TimeoutSeconds = TimeoutSeconds,

            RemoveNonAdherents = RemoveNonAdherents,
            MinArea = MinArea,
            MaxCircularity = MaxCircularity,
            MaxIntensity = MaxIntensity,
            MinVariance = MinVariance
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

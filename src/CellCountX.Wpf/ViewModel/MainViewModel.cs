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
    // 境界細胞除去（4方向 + マージン）
    // ---------------------------------------------------------
    public bool UseEdgeFilter
    {
        get => Properties.Settings.Default.UseEdgeFilter;
        set
        {
            Properties.Settings.Default.UseEdgeFilter = value;
            Properties.Settings.Default.Save();
            OnPropertyChanged(nameof(UseEdgeFilter));
        }
    }

    public bool UseEdgeTop
    {
        get => Properties.Settings.Default.UseEdgeTop;
        set
        {
            Properties.Settings.Default.UseEdgeTop = value;
            Properties.Settings.Default.Save();
            OnPropertyChanged(nameof(UseEdgeTop));
        }
    }

    public bool UseEdgeBottom
    {
        get => Properties.Settings.Default.UseEdgeBottom;
        set
        {
            Properties.Settings.Default.UseEdgeBottom = value;
            Properties.Settings.Default.Save();
            OnPropertyChanged(nameof(UseEdgeBottom));
        }
    }

    public bool UseEdgeLeft
    {
        get => Properties.Settings.Default.UseEdgeLeft;
        set
        {
            Properties.Settings.Default.UseEdgeLeft = value;
            Properties.Settings.Default.Save();
            OnPropertyChanged(nameof(UseEdgeLeft));
        }
    }

    public bool UseEdgeRight
    {
        get => Properties.Settings.Default.UseEdgeRight;
        set
        {
            Properties.Settings.Default.UseEdgeRight = value;
            Properties.Settings.Default.Save();
            OnPropertyChanged(nameof(UseEdgeRight));
        }
    }

    public int EdgeMargin
    {
        get => Properties.Settings.Default.EdgeMargin;
        set
        {
            Properties.Settings.Default.EdgeMargin = value;
            Properties.Settings.Default.Save();
            OnPropertyChanged(nameof(EdgeMargin));
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
    private readonly PythonServer _pythonServer;   // ★ 追加：バージョン取得のため保持
    private CancellationTokenSource? _cts;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        // 設定読み込み
        UseGpu = Properties.Settings.Default.UseGpu;

        // 詳細設定で TimeoutSeconds が設定されていればそれを使う。0 以下なら自動値。
        var savedTimeout = Properties.Settings.Default.TimeoutSeconds;
        TimeoutSeconds = savedTimeout > 0 ? savedTimeout : GetAutoTimeout(UseGpu);

        // ---------------------------------------------------------
        // PythonServer → PythonClient → BatchProcessor
        // ---------------------------------------------------------
        _pythonServer = new PythonServer();                 // ★ 追加：保持する
        var pythonClient = new PythonClient(_pythonServer);
        _processor = new BatchProcessor(pythonClient);

        // イベント購読
        _processor.Log += msg => AppendLog(msg);
        _processor.Progress += v => ProgressValue = v;
        _processor.Completed += _ =>
        {
            IsRunning = false;
            AppendLog("処理が完了しました。");
        };

        // ---------------------------------------------------------
        // ★ 起動時に Cellpose バージョンをログ出力
        // ---------------------------------------------------------
        Task.Run(() =>
        {
            var ver = _pythonServer.GetCellposeVersion();
            if (!string.IsNullOrEmpty(ver))
                AppendLog($"Cellpose バージョン: {ver}");
        });

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
            UseEdgeFilter = UseEdgeFilter,
            UseEdgeTop = UseEdgeTop,
            UseEdgeBottom = UseEdgeBottom,
            UseEdgeLeft = UseEdgeLeft,
            UseEdgeRight = UseEdgeRight,
            EdgeMargin = EdgeMargin,

            // Cellpose モデルパスを Python に渡す
            CellposeModelPath = Properties.Settings.Default.CellposeModelPath
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

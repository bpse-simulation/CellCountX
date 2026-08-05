using CellCountX.Wpf.Logic;
using CellCountX.Wpf.Model;
using System.ComponentModel;
using System.IO;
using System.Windows;
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

    // ---------------------------------------------------------
    // GPU
    // ---------------------------------------------------------
    private bool _useGpu;
    public bool UseGpu
    {
        get => _useGpu;
        set
        {
            if (_useGpu == value) return;

            var oldUseGpu = _useGpu;
            _useGpu = value;
            Properties.Settings.Default.UseGpu = _useGpu;
            Properties.Settings.Default.Save();
            OnPropertyChanged(nameof(UseGpu));

            // 以前が自動タイムアウト値だった場合のみ、新しい自動値に追従させる
            if (TimeoutSeconds == GetAutoTimeout(oldUseGpu) || TimeoutSeconds <= 0)
            {
                TimeoutSeconds = GetAutoTimeout(_useGpu);
            }
        }
    }

    // ---------------------------------------------------------
    // GPU 利用可能かどうか（Python 環境チェックで設定）
    // ---------------------------------------------------------
    private bool _gpuAvailable;
    public bool GpuAvailable
    {
        get => _gpuAvailable;
        set
        {
            _gpuAvailable = value;
            OnPropertyChanged(nameof(GpuAvailable));
        }
    }

    // ---------------------------------------------------------
    // タイムアウト
    // ---------------------------------------------------------
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
    // Python 環境チェック
    // ---------------------------------------------------------
    private bool _isCheckingPython;
    public bool IsCheckingPython
    {
        get => _isCheckingPython;
        set
        {
            _isCheckingPython = value;
            OnPropertyChanged(nameof(IsCheckingPython));
            StartBatchCommand.RaiseCanExecuteChanged();
        }
    }

    // ---------------------------------------------------------
    // Python 利用可能かどうか（Python 環境チェックで設定）
    // ---------------------------------------------------------
    private bool _isPythonAvailable;
    public bool IsPythonAvailable
    {
        get => _isPythonAvailable;
        set
        {
            _isPythonAvailable = value;
            OnPropertyChanged(nameof(IsPythonAvailable));
            StartBatchCommand.RaiseCanExecuteChanged();
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
    private readonly PythonServer _pythonServer;
    private CancellationTokenSource? _cts;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        // 詳細設定から読み込む
        UseGpu = Properties.Settings.Default.UseGpu;

        // 詳細設定で TimeoutSeconds が設定されていればそれを使う。0 以下なら自動値。
        var savedTimeout = Properties.Settings.Default.TimeoutSeconds;
        TimeoutSeconds = savedTimeout > 0 ? savedTimeout : GetAutoTimeout(UseGpu);

        // PythonServer → PythonClient → BatchProcessor
        _pythonServer = new PythonServer();
        var pythonClient = new PythonClient(_pythonServer);
        _processor = new BatchProcessor(pythonClient);

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
        StartBatchCommand = new RelayCommand(
            async _ => await StartBatchAsync(),
            _ => !IsRunning && !IsCheckingPython && IsPythonAvailable
        );
        CancelBatchCommand = new RelayCommand(_ => CancelBatch(), _ => IsRunning);
    }

    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    internal int GetAutoTimeout(bool useGpu)
        => useGpu ? 300 : 900;

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

        UseGpu = Properties.Settings.Default.UseGpu; // ユーザー設定は保持

        // 非接着細胞除去パラメータを含めて Python に渡す
        var req = new BatchRequest
        {
            InputFolder = InputFolder,
            OutputFolder = OutputFolder,
            UseGpu = GpuAvailable && UseGpu,  // 実際に使うかどうかはここで決める
            TimeoutSeconds = TimeoutSeconds,

            // Cellpose モデルパスを Python に渡す
            CellposeModelPath = Properties.Settings.Default.CellposeModelPath,

            // 境界細胞除去
            UseEdgeFilter = Properties.Settings.Default.UseEdgeFilter,
            UseEdgeTop = Properties.Settings.Default.UseEdgeTop,
            UseEdgeBottom = Properties.Settings.Default.UseEdgeBottom,
            UseEdgeLeft = Properties.Settings.Default.UseEdgeLeft,
            UseEdgeRight = Properties.Settings.Default.UseEdgeRight,
            EdgeMargin = Properties.Settings.Default.EdgeMargin
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
    // Python チェックは非同期で実行（UI を止めない）
    // ---------------------------------------------------------
    private void RunPythonEnvironmentCheck()
    {
        IsCheckingPython = true;
        AppendLog("Python 環境チェック中…");

        Task.Run(() =>
        {
            // Python チェック
            var result = _pythonServer.CheckPythonEnvironment();

            // Python が無い場合は GPU チェックをスキップ
            string? ver = null;
            bool? gpu = null;

            if (result.IsAvailable)
            {
                (ver, gpu) = _pythonServer.GetCellposeVersion();
            }

            // UI スレッドにまとめて戻す
            Application.Current.Dispatcher.Invoke(() =>
            {
                AppendLog(result.Message);
                IsPythonAvailable = result.IsAvailable;

                if (!IsPythonAvailable)
                {
                    AppendLog("Python 環境が存在しないため開始できません。");
                    IsCheckingPython = false;
                    return;
                }

                // Cellpose バージョン
                if (!string.IsNullOrEmpty(ver))
                    AppendLog($"Cellpose バージョン: {ver}");

                // GPU 判定
                GpuAvailable = gpu ?? false;

                if (!(gpu ?? false))
                {
                    UseGpu = false;
                    AppendLog("GPU が利用できないため CPU に切り替えます。");
                }

                AppendLog("Python 環境チェック完了");
                IsCheckingPython = false;
            });
        });
    }

    // ---------------------------------------------------------
    // ログ追加
    // ---------------------------------------------------------
    private void AppendLog(string message)
    {
        LogText += $"{DateTime.Now:HH:mm:ss}  {message}\n";
    }

    // ---------------------------------------------------------
    // Window Loaded 時に呼び出す
    // ---------------------------------------------------------
    public void OnWindowLoaded()
    {
        AppendLog("CellCountX 起動");
        RunPythonEnvironmentCheck();
    }
}

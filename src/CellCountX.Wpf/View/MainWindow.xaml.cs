using CellCountX.Wpf.ViewModel;
using System.ComponentModel;
using System.Windows;

namespace CellCountX.Wpf.View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Closed += OnClosed;

        // LogText の変更を監視してスクロール
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    // ---------------------------------------------------------
    // Window Loaded
    // ---------------------------------------------------------
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // ViewModel に初期ログを任せる
        _viewModel.AppendStartupLog();
    }

    // ---------------------------------------------------------
    // Window Closing - 設定保存 & 実行中ならキャンセル
    // ---------------------------------------------------------
    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            // 設定保存
            Properties.Settings.Default.UseGpu = vm.UseGpu;
            Properties.Settings.Default.TimeoutSeconds = vm.TimeoutSeconds;
            Properties.Settings.Default.Save();

            // 実行中ならキャンセルして Python プロセスを Kill
            if (vm.IsRunning)
            {
                vm.CancelBatchCommand.Execute(null);
            }
        }

        base.OnClosing(e);
    }

    // ---------------------------------------------------------
    // Window Closed
    // ---------------------------------------------------------
    private void OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel is IDisposable disposable)
            disposable.Dispose();
    }

    // -----------------------------
    // LogText 更新時に自動スクロール
    // -----------------------------
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.LogText))
        {
            // UI スレッドで遅延実行してスクロールを安定化
            LogScrollViewer.Dispatcher.InvokeAsync(() =>
            {
                LogScrollViewer.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    // -----------------------------
    // 終了メニュークリック
    // -----------------------------
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        // ViewModel を取得
        if (DataContext is MainViewModel vm)
        {
            // 実行中ならキャンセル
            if (vm.IsRunning)
            {
                vm.CancelBatchCommand.Execute(null);
            }
        }

        // ウィンドウを閉じる
        this.Close();
    }

    // -----------------------------
    // 詳細設定ウィンドウを開く
    // -----------------------------
    private void OpenAdvancedSettings(object sender, RoutedEventArgs e)
    {
        var dlg = new AdvancedSettingsWindow
        {
            Owner = this
        };

        if (dlg.ShowDialog() == true)
        {
            // 設定が保存されたので ViewModel に反映
            if (DataContext is MainViewModel vm)
            {
                var saved = Properties.Settings.Default.TimeoutSeconds;
                vm.TimeoutSeconds = saved > 0 ? saved : vm.GetAutoTimeout(vm.UseGpu);
            }
        }
    }

    // -----------------------------
    // バージョン情報ウィンドウを開く
    // -----------------------------
    private void OpenAboutWindow(object sender, RoutedEventArgs e)
    {
        var dlg = new AboutWindow
        {
            Owner = this
        };
        dlg.ShowDialog();
    }
}

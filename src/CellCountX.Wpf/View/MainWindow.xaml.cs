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
}

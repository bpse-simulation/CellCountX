using CellCountX.Wpf.ViewModel;
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
        // 将来 Dispose が必要になった場合のために残す
        if (_viewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
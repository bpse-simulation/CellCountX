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

        // ViewModel を生成して DataContext に設定
        _viewModel = new MainViewModel();
        this.DataContext = _viewModel;

        // ロードイベント
        this.Loaded += MainWindow_Loaded;

        // クローズイベント
        this.Closed += MainWindow_Closed;
    }

    // ---------------------------------------------------------
    // Window Loaded
    // ---------------------------------------------------------
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 必要なら初期化処理をここに
        // 例: ログに起動メッセージ
        _viewModel.LogText += "CellCountX 起動\n";
    }

    // ---------------------------------------------------------
    // Window Closed
    // ---------------------------------------------------------
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // ViewModel に Dispose が必要ならここで呼ぶ
        // _viewModel.Dispose();
    }
}

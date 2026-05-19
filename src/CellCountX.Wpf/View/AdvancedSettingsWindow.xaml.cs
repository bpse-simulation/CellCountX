using System.Windows;
using CellCountX.Wpf.ViewModel;

namespace CellCountX.Wpf.View;

/// <summary>
/// AdvancedSettingsWindow.xaml の相互作用ロジック
/// </summary>
public partial class AdvancedSettingsWindow : Window
{
    public AdvancedSettingsViewModel ViewModel { get; }

    public AdvancedSettingsWindow()
    {
        InitializeComponent();
        ViewModel = new AdvancedSettingsViewModel();
        DataContext = ViewModel;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Save();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

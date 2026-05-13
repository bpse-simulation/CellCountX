using System.Reflection;
using System.Windows;

namespace CellCountX.Wpf.View;

/// <summary>
/// AboutWindow.xaml の相互作用ロジック
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        var displayVersion = version.Split('+')[0];

        VersionText.Text = $"Version {displayVersion}";
    }
}
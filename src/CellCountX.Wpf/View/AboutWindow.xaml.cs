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

        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        VersionText.Text = $"Version {info}";
    }
}
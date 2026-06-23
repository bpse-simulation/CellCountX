using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Win32;

namespace CellCountX.Wpf.ViewModel;

public class AdvancedSettingsViewModel : INotifyPropertyChanged
{
    private int _timeoutSeconds;
    private string? _cellposeModelPath;

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            _timeoutSeconds = value;
            OnPropertyChanged(nameof(TimeoutSeconds));
        }
    }

    public string? CellposeModelPath
    {
        get => _cellposeModelPath;
        set
        {
            _cellposeModelPath = value;
            OnPropertyChanged(nameof(CellposeModelPath));
        }
    }

    // コマンド
    public ICommand BrowseModelCommand { get; }

    public AdvancedSettingsViewModel()
    {
        TimeoutSeconds = Properties.Settings.Default.TimeoutSeconds;
        CellposeModelPath = Properties.Settings.Default.CellposeModelPath;

        BrowseModelCommand = new RelayCommand(_ => BrowseModel());
    }

    private void BrowseModel()
    {
        var dlg = new OpenFileDialog()
        {
            Filter = "Cellpose Model (*.npy;*)|*.npy;*",
            Title = "Cellpose モデルファイルを選択"
        };
        if (CellposeModelPath != null)
        {
            try
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(CellposeModelPath);
                dlg.FileName = System.IO.Path.GetFileName(CellposeModelPath);
            }
            catch { }
        }

        if (dlg.ShowDialog() == true)
        {
            CellposeModelPath = dlg.FileName;
        }
    }

    public void Save()
    {
        Properties.Settings.Default.TimeoutSeconds = TimeoutSeconds;
        Properties.Settings.Default.CellposeModelPath = CellposeModelPath;
        Properties.Settings.Default.Save();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

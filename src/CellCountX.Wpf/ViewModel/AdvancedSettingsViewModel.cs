using System.ComponentModel;

namespace CellCountX.Wpf.ViewModel;

public class AdvancedSettingsViewModel : INotifyPropertyChanged
{
    private int _timeoutSeconds;

    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            _timeoutSeconds = value;
            OnPropertyChanged(nameof(TimeoutSeconds));
        }
    }

    public AdvancedSettingsViewModel()
    {
        TimeoutSeconds = Properties.Settings.Default.TimeoutSeconds;
    }

    public void Save()
    {
        Properties.Settings.Default.TimeoutSeconds = TimeoutSeconds;
        Properties.Settings.Default.Save();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

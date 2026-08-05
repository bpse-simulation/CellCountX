using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Input;

namespace CellCountX.Wpf.ViewModel;

public class AdvancedSettingsViewModel : INotifyPropertyChanged
{
    private int _timeoutSeconds;
    private string? _cellposeModelPath;
    private bool _useEdgeFilter;
    private bool _useEdgeTop;
    private bool _useEdgeBottom;
    private bool _useEdgeLeft;
    private bool _useEdgeRight;
    private int _edgeMargin;

    // -----------------------------
    // Timeout
    // -----------------------------
    public int TimeoutSeconds
    {
        get => _timeoutSeconds;
        set
        {
            if (value < 0)
                value = 0;  // 0 は自動設定扱い

            _timeoutSeconds = value;
            OnPropertyChanged(nameof(TimeoutSeconds));
        }
    }

    // -----------------------------
    // Model
    // -----------------------------
    public string? CellposeModelPath
    {
        get => _cellposeModelPath;
        set
        {
            _cellposeModelPath = value;
            OnPropertyChanged(nameof(CellposeModelPath));
        }
    }

    // -----------------------------
    // 境界細胞除去
    // -----------------------------
    public bool UseEdgeFilter
    {
        get => _useEdgeFilter;
        set
        {
            _useEdgeFilter = value;
            OnPropertyChanged(nameof(UseEdgeFilter));
        }
    }

    public bool UseEdgeTop
    {
        get => _useEdgeTop;
        set
        {
            _useEdgeTop = value;
            OnPropertyChanged(nameof(UseEdgeTop));
        }
    }

    public bool UseEdgeBottom
    {
        get => _useEdgeBottom;
        set
        {
            _useEdgeBottom = value;
            OnPropertyChanged(nameof(UseEdgeBottom));
        }
    }

    public bool UseEdgeLeft
    {
        get => _useEdgeLeft;
        set
        {
            _useEdgeLeft = value;
            OnPropertyChanged(nameof(UseEdgeLeft));
        }
    }

    public bool UseEdgeRight
    {
        get => _useEdgeRight;
        set
        {
            _useEdgeRight = value;
            OnPropertyChanged(nameof(UseEdgeRight));
        }
    }

    public int EdgeMargin
    {
        get => _edgeMargin;
        set
        {
            _edgeMargin = value;
            OnPropertyChanged(nameof(EdgeMargin));
        }
    }

    // コマンド
    public ICommand BrowseModelCommand { get; }

    public AdvancedSettingsViewModel()
    {
        TimeoutSeconds = Properties.Settings.Default.TimeoutSeconds;
        CellposeModelPath = Properties.Settings.Default.CellposeModelPath;
        UseEdgeFilter = Properties.Settings.Default.UseEdgeFilter;
        UseEdgeTop = Properties.Settings.Default.UseEdgeTop;
        UseEdgeBottom = Properties.Settings.Default.UseEdgeBottom;
        UseEdgeLeft = Properties.Settings.Default.UseEdgeLeft;
        UseEdgeRight = Properties.Settings.Default.UseEdgeRight;
        EdgeMargin = Properties.Settings.Default.EdgeMargin;

        BrowseModelCommand = new RelayCommand(_ => BrowseModel());
    }

    private void BrowseModel()
    {
        var dlg = new OpenFileDialog()
        {
            Filter = "Cellpose Model (*.npy)|*.npy|すべてのファイル (*.*)|*.*",
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
        // タイムアウト秒数
        Properties.Settings.Default.TimeoutSeconds = TimeoutSeconds;

        // Cellpose モデルパス
        Properties.Settings.Default.CellposeModelPath = CellposeModelPath;

        // 境界細胞除去
        Properties.Settings.Default.UseEdgeFilter = UseEdgeFilter;
        Properties.Settings.Default.UseEdgeTop = UseEdgeTop;
        Properties.Settings.Default.UseEdgeBottom = UseEdgeBottom;
        Properties.Settings.Default.UseEdgeLeft = UseEdgeLeft;
        Properties.Settings.Default.UseEdgeRight = UseEdgeRight;
        Properties.Settings.Default.EdgeMargin = EdgeMargin;

        Properties.Settings.Default.Save();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

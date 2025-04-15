//ViewModels/SevenLayerProtectionViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;
using RealTimeProtection;

public class SevenLayerProtectionViewModel : INotifyPropertyChanged
{
    private ObservableCollection<ProtectedFolder> _monitoringFolders;
    private ObservableCollection<ProtectedFolder> _premiumFolders;
    private FolderMonitor _monitor;

    public ObservableCollection<ProtectedFolder> MonitoringFolders
    {
        get => _monitoringFolders;
        set
        {
            _monitoringFolders = value;
            OnPropertyChanged(nameof(MonitoringFolders));
        }
    }

    public ObservableCollection<ProtectedFolder> PremiumFolders
    {
        get => _premiumFolders;
        set
        {
            _premiumFolders = value;
            OnPropertyChanged(nameof(PremiumFolders));
        }
    }

    public SevenLayerProtectionViewModel()
    {
        MonitoringFolders = new ObservableCollection<ProtectedFolder>();
        PremiumFolders = new ObservableCollection<ProtectedFolder>();
    }

    public void AddMonitoringFolder()
    {
        using (var dialog = new FolderBrowserDialog())
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                MonitoringFolders.Add(new ProtectedFolder(dialog.SelectedPath));
                _monitor.reload();
            }
        }
    }

    public void AddPremiumFolder()
    {
        using (var dialog = new FolderBrowserDialog())
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                PremiumFolders.Add(new ProtectedFolder(dialog.SelectedPath, true));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
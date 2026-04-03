using System.ComponentModel;
using System.Runtime.CompilerServices;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.UI.Wpf.ViewModels.Sections;

public sealed class HistorySectionViewModel : INotifyPropertyChanged
{
    private IReadOnlyCollection<RecentHistoryPresentation> _recentHistory = Array.Empty<RecentHistoryPresentation>();

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyCollection<RecentHistoryPresentation> RecentHistory
    {
        get => _recentHistory;
        set
        {
            _recentHistory = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

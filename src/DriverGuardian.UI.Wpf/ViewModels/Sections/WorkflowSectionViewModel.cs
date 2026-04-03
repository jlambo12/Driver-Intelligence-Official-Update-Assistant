using System.ComponentModel;
using System.Runtime.CompilerServices;
using DriverGuardian.UI.Wpf.Models;

namespace DriverGuardian.UI.Wpf.ViewModels.Sections;

public sealed class WorkflowSectionViewModel : INotifyPropertyChanged
{
    private MainUiState _state;
    private bool _showSecondaryRecommendations;

    public WorkflowSectionViewModel(MainUiState initialState)
    {
        _state = initialState;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainUiState State
    {
        get => _state;
        set
        {
            if (_state == value)
            {
                return;
            }

            _state = value;
            OnPropertyChanged();
        }
    }

    public bool ShowSecondaryRecommendations
    {
        get => _showSecondaryRecommendations;
        set
        {
            if (_showSecondaryRecommendations == value)
            {
                return;
            }

            _showSecondaryRecommendations = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

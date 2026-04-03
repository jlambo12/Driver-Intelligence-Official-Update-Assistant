using System.Windows.Input;

namespace DriverGuardian.UI.Wpf.Commands;

public sealed class AsyncRelayCommand(Func<Task> execute, Action<Exception>? onError = null) : ICommand
{
    private bool _isRunning;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isRunning;

    public async void Execute(object? parameter)
    {
        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }

    public async Task ExecuteAsync(object? parameter = null)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await execute();
        }
        finally
        {
            _isRunning = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

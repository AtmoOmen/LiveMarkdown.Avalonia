using System.Windows.Input;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A simple implementation of <see cref="ICommand"/> that takes delegates for execution and can-execute logic.
/// </summary>
/// <param name="execute"></param>
/// <param name="canExecute"></param>
internal class SimpleCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        execute();
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
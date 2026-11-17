using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Общая модель диалога: отмена и запрос закрытия без зависимости от окна Avalonia.</summary>
public abstract class DialogViewModelBase : AsyncViewModelBase
{
    protected DialogViewModelBase(object? cancelResult = null)
    {
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(cancelResult));
    }

    public event Action<object?>? CloseRequested;
    public ICommand CancelCommand
    {
        get;
    }

    protected void RequestClose(object? result)
    {
        CloseRequested?.Invoke(result);
    }
}

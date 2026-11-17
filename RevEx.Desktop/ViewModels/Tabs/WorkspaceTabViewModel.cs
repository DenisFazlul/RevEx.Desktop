using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using RevEx.Desktop.ViewModels.Common;

namespace RevEx.Desktop.ViewModels.Tabs;

/// <summary>Основа рабочей вкладки: заголовок, выбор, загрузка и освобождение ресурсов при закрытии.</summary>
public abstract partial class WorkspaceTabViewModel : AsyncViewModelBase, IDisposable
{
    [ObservableProperty]
    private bool _isSelected;

    protected WorkspaceTabViewModel(
        string title,
        bool canClose,
        object? key = null)
    {
        _title = title;
        CanClose = canClose;
        Key = key;
    }

    [ObservableProperty]
    private string _title;
    public bool CanClose
    {
        get;
    }
    public object? Key
    {
        get;
    }

    public virtual Task ActivateAsync() => Task.CompletedTask;

    public async Task ActivateSafelyAsync()
    {
        try
        {
            await ActivateAsync();
        }
        catch (Exception exception) when (IsExpectedError(exception))
        {
            SetError($"Не удалось открыть вкладку: {exception.Message}");
        }
    }

    public virtual void Dispose()
    {
    }
}

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Общее состояние экрана: загрузка, сохранение и понятные сообщения об ошибках.</summary>
public abstract partial class AsyncViewModelBase : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private bool _isSaving;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool IsBusy => IsLoading || IsSaving;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected Task<bool> RunLoadAsync(Func<Task> action, string failureMessage)
    {
        return RunOperationAsync(action, failureMessage, true);
    }

    protected Task<bool> RunSaveAsync(Func<Task> action, string failureMessage)
    {
        return RunOperationAsync(action, failureMessage, false);
    }

    protected void SetError(string message)
    {
        ErrorMessage = message;
        Message = message;
    }

    protected void ClearMessage()
    {
        Message = null;
        ErrorMessage = null;
    }

    protected static bool IsExpectedError(Exception exception)
    {
        return exception is HttpRequestException or IOException or UnauthorizedAccessException
            or OperationCanceledException or InvalidOperationException;
    }

    protected virtual void OnLoadingStateChanged()
    {
    }

    protected virtual void OnBusyStateChanged()
    {
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnLoadingStateChanged();
        OnBusyStateChanged();
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnBusyStateChanged();
    }

    private async Task<bool> RunOperationAsync(Func<Task> action, string failureMessage, bool loading)
    {
        if (loading)
        {
            IsLoading = true;
        }
        else
        {
            IsSaving = true;
        }

        ClearMessage();
        try
        {
            await action();
            return true;
        }
        catch (Exception exception) when (IsExpectedError(exception))
        {
            SetError($"{failureMessage}: {exception.Message}");
            return false;
        }
        finally
        {
            if (loading)
            {
                IsLoading = false;
            }
            else
            {
                IsSaving = false;
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Services.Updates;
using Velopack;

namespace RevEx.Desktop.ViewModels.Updates;

public partial class UpdateWindowViewModel(IAppUpdateService service, UpdateInfo update)
    : ObservableObject
{
    private readonly TaskCompletionSource<bool> _decision = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private CancellationTokenSource? _downloadCancellation;
    private bool _continueRequested;

    public string CurrentVersion => service.CurrentVersion;
    public string NewVersion => update.TargetFullRelease.Version.ToString();
    public Task<bool> Decision => _decision.Task;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUpdate))]
    private bool _isDownloading;

    [ObservableProperty] private int _progress;
    [ObservableProperty] private string? _errorMessage;

    public bool CanUpdate => !IsDownloading;

    [RelayCommand]
    private void ContinueWithoutUpdate()
    {
        _continueRequested = true;
        if (IsDownloading)
            _downloadCancellation?.Cancel();
        else
            _decision.TrySetResult(false);
    }

    [RelayCommand]
    private async Task UpdateAsync()
    {
        if (IsDownloading || _decision.Task.IsCompleted)
            return;

        ErrorMessage = null;
        Progress = 0;
        IsDownloading = true;
        using var cancellation = new CancellationTokenSource();
        _downloadCancellation = cancellation;
        try
        {
            await service.DownloadAsync(update,
                value => Dispatcher.UIThread.Post(() => Progress = value),
                cancellation.Token);
            _decision.TrySetResult(!_continueRequested);
        }
        catch (OperationCanceledException) when (_continueRequested)
        {
            _decision.TrySetResult(false);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Не удалось скачать обновление: {exception.Message}";
            if (_continueRequested)
                _decision.TrySetResult(false);
        }
        finally
        {
            _downloadCancellation = null;
            IsDownloading = false;
        }
    }
}

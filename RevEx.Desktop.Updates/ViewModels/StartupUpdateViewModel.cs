using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Configuration;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Updates.Services;

namespace RevEx.Desktop.Updates.ViewModels;

public partial class StartupUpdateViewModel(DesktopUpdateService updates, IAppSettings settings,
    IRevExUserSettingsStore userSettings) : ObservableObject
{
    private readonly TaskCompletionSource<bool> _decision = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private DesktopUpdateCheck? _check;
    private CancellationTokenSource? _operation;
    private bool? _afterCancellation;
    public Task<bool> Decision => _decision.Task;
    public string CurrentVersion => updates.CurrentVersion;

    [ObservableProperty] private string _serverAddress = settings.ApiPath;
    [ObservableProperty] private string _status = "Проверяем совместимость с backend…";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _recommendedVersion;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int _progress;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _canContinue;
    [ObservableProperty] private bool _hasUpdate;

    [RelayCommand]
    private async Task CheckAsync()
    {
        if (IsBusy || _decision.Task.IsCompleted) return;
        IsBusy = true; CanContinue = false; HasUpdate = false; Error = null; _check = null;
        using var operation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        _operation = operation;
        try
        {
            _check = await updates.CheckAsync(ServerAddress, operation.Token);
            settings.ApiPath = ServerAddress.TrimEnd('/') + "/";
            var saved = userSettings.Load();
            userSettings.Save(new RevExUserSettings { ApiPath = settings.ApiPath, ConnectorRegistrationAddress = saved.ConnectorRegistrationAddress });
            CanContinue = _check.Compatible;
            RecommendedVersion = _check.Response.TargetVersion;
            Notes = _check.Response.Notes;
            HasUpdate = _check.HasUpdate;
            Status = CanContinue ? "Доступна рекомендованная версия. Можно обновиться или продолжить работу."
                : "Текущая версия не поддерживается backend. Для работы требуется обновление.";
            if (CanContinue && !HasUpdate) _decision.TrySetResult(true);
        }
        catch (Exception exception)
        {
            Status = "Не удалось проверить совместимость. Проверьте адрес сервера и повторите попытку.";
            Error = exception is OperationCanceledException ? "Проверка отменена или сервер не ответил вовремя." : exception.Message;
        }
        finally { IsBusy = false; _operation = null; CompleteCancellation(); }
    }

    [RelayCommand]
    private async Task UpdateAsync()
    {
        if (IsBusy || _check == null || !HasUpdate || _decision.Task.IsCompleted) return;
        IsBusy = true; IsDownloading = true; Error = null; Progress = 0;
        Status = "Скачиваем пакет с backend. После установки приложение перезапустится.";
        using var operation = new CancellationTokenSource();
        _operation = operation;
        try
        {
            await updates.InstallAsync(_check, value => Dispatcher.UIThread.Post(() => Progress = value), operation.Token);
            _decision.TrySetResult(false);
        }
        catch (Exception exception)
        {
            Error = exception is OperationCanceledException ? "Скачивание отменено." : $"Обновление не установлено: {exception.Message}";
            Status = "Можно повторить обновление или закрыть приложение.";
        }
        finally { IsDownloading = false; IsBusy = false; _operation = null; CompleteCancellation(); }
    }

    [RelayCommand]
    private void ContinueWithoutUpdate()
    {
        if (CanContinue) Finish(true);
    }

    public void RequestClose() => Finish(false);
    private void Finish(bool continueStartup)
    {
        if (IsBusy) { _afterCancellation = continueStartup; _operation?.Cancel(); }
        else _decision.TrySetResult(continueStartup);
    }
    private void CompleteCancellation()
    {
        if (_afterCancellation is { } decision) _decision.TrySetResult(decision && CanContinue);
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Изменение статуса публикации выбранной версии контента.</summary>
public partial class ContentDetailsViewModel
{
    [ObservableProperty]
    private PublishStatusOption? _selectedPublishStatus;

    [ObservableProperty]
    private bool _isSavingPublishStatus;

    public IReadOnlyList<PublishStatusOption> PublishStatuses { get; } =
    [
        new(PublishStatus.Draft, "Черновик"),
        new(PublishStatus.Ready, "Готов"),
        new(PublishStatus.Withdrawn, "Снят")
    ];

    public bool CanChangePublishStatus => CanEdit && HasSelectedVersion && !IsLoading && !IsVersionBusy;
    public bool CanSavePublishStatus => CanChangePublishStatus && SelectedPublishStatus is not null
        && SelectedPublishStatus.Value != SelectedVersion?.PublishStatus;

    partial void OnSelectedPublishStatusChanged(PublishStatusOption? value)
    {
        NotifyPublishStatusStateChanged();
    }

    partial void OnIsSavingPublishStatusChanged(bool value)
    {
        NotifyVersionStateChanged();
    }

    private void NotifyPublishStatusStateChanged()
    {
        OnPropertyChanged(nameof(CanChangePublishStatus));
        OnPropertyChanged(nameof(CanSavePublishStatus));
        SavePublishStatusCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSavePublishStatus))]
    private async Task SavePublishStatusAsync()
    {
        var version = SelectedVersion;
        var status = SelectedPublishStatus;
        if (!CanSavePublishStatus || version is null || status is null)
        {
            return;
        }

        IsSavingPublishStatus = true;
        VersionErrorMessage = null;
        VersionMessage = null;
        try
        {
            await _apiService.ContentVersions.UpdatePublishStatusAsync(version.Id, status.Value);
            var updated = await _apiService.ContentVersions.GetByIdAsync(version.Id)
                ?? throw new HttpRequestException("Версия больше не существует.");
            ReplaceVersion(updated);
            SelectedVersion = updated;
            VersionMessage = $"Статус публикации изменён на «{status.Name}».";
            await RefreshHistoryAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            VersionErrorMessage = $"Не удалось сохранить статус публикации: {exception.Message}";
        }
        finally
        {
            IsSavingPublishStatus = false;
        }
    }
}

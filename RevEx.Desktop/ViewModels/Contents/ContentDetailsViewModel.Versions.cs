using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentFile;
using RevEx.Domain.Contracts.ContentHistory;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Contracts.Tag;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Connector.Contracts;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Configuration;
using RevEx.Connector.Client;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Работа карточки с версиями и их файлами: выбор, добавление и удаление.</summary>
public partial class ContentDetailsViewModel
{
    public void AddOrUpdateVersion(ContentVersionDto version)
    {
        var existing = Versions.FirstOrDefault(item => item.Id == version.Id);
        if (existing is not null)
        {
            Versions.Remove(existing);
        }

        Versions.Insert(0, version);
        NotifyCollectionStateChanged();
        _ = SelectVersionAsync(version);
    }

    [RelayCommand]
    private async Task SelectVersionAsync(ContentVersionDto version)
    {
        if (IsSavingPublishStatus)
        {
            return;
        }

        try
        {
            IsLoadingVersion = true;
            VersionErrorMessage = null;
            VersionMessage = null;
            UpdateVersionMetadata(Array.Empty<ContentFileDto>());
            SelectedVersion = version;
            var files = await _apiService.ContentFiles.GetAllAsync();
            if (SelectedVersion?.Id != version.Id)
            {
                return;
            }
            ReplaceVersionFiles(files.Where(item => item.ContentVersionId == version.Id));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            if (SelectedVersion?.Id == version.Id)
            {
                VersionFiles.Clear();
                UpdateVersionMetadata(Array.Empty<ContentFileDto>());
                VersionErrorMessage = $"Не удалось загрузить файлы версии: {exception.Message}";
            }
        }
        finally
        {
            if (SelectedVersion?.Id == version.Id)
            {
                IsLoadingVersion = false;
                NotifyVersionStateChanged();
            }
        }
    }

    public async Task AddVersionFileAsync(string path, string fileName)
    {
        if (SelectedVersion is null)
        {
            return;
        }

        try
        {
            IsSavingVersionFile = true;
            VersionErrorMessage = null;
            VersionMessage = null;
            await using var stream = File.OpenRead(path);
            var updated = await _apiService.ContentVersions.UploadFileAsync(
                SelectedVersion.Id, stream, fileName, SelectedFileRole.Value);
            ReplaceVersion(updated);
            await SelectVersionAsync(updated);
            VersionMessage = "Файл добавлен в версию.";
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            VersionErrorMessage = $"Не удалось добавить файл: {exception.Message}";
        }
        finally
        {
            IsSavingVersionFile = false;
            NotifyVersionStateChanged();
        }
    }

    public async Task DeleteVersionFileAsync(ContentVersionFileItemViewModel file)
    {
        if (SelectedVersion is null)
        {
            return;
        }

        try
        {
            IsSavingVersionFile = true;
            VersionErrorMessage = null;
            VersionMessage = null;
            await _apiService.ContentVersions.DeleteFileAsync(SelectedVersion.Id, file.Id);
            VersionFiles.Remove(file);
            UpdateVersionMetadata(_versionMetadataFiles.Where(item => item.Id != file.Id));
            SelectedVersion = SelectedVersion with
            {
                FileIds = SelectedVersion.FileIds.Where(id => id != file.Id).ToArray()
            };
            ReplaceVersion(SelectedVersion);
            VersionMessage = "Файл удалён из версии.";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            VersionErrorMessage = $"Не удалось удалить файл: {exception.Message}";
        }
        finally
        {
            IsSavingVersionFile = false;
            NotifyVersionStateChanged();
        }
    }

}

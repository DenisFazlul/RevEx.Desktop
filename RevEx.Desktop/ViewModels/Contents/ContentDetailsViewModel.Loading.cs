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

/// <summary>Подготовка локальных файлов и отправка выбранной версии в коннектор.</summary>
public partial class ContentDetailsViewModel
{
    public IReadOnlyCollection<ConnectorDescriptor> GetAvailableConnectors() =>
        _connectorRegistry.GetAll();

    public void QueueVersionForLoading(ConnectorDescriptor connector)
    {
        if (SelectedVersion is null || VersionFiles.Count == 0)
        {
            return;
        }

        VersionErrorMessage = null;
        var selectedVersion = SelectedVersion;
        var versionFiles = VersionFiles.ToArray();
        var targetDirectory = Path.Combine(
            RevExSettingsPaths.DownloadsDirectory,
            CreateSafeDirectoryName(Name, $"content-{Id}"),
            CreateSafeDirectoryName(selectedVersion.Name, $"version-{selectedVersion.Id}"));

        var filesToLoad = new List<QueuedVersionFile>(versionFiles.Length);
        foreach (var file in versionFiles)
        {
            var loadingId = Guid.NewGuid();
            var hasDuplicateName = versionFiles.Count(item =>
                string.Equals(item.Name, file.Name, StringComparison.OrdinalIgnoreCase)) > 1;
            var path = GetVersionFilePath(targetDirectory, file.Name, file.Id, hasDuplicateName);
            filesToLoad.Add(new QueuedVersionFile(file.Id, loadingId, file.Name, path));
        }

        VersionMessage = $"В очередь {connector.Name} добавлено файлов: {filesToLoad.Count}.";
        _ = Task.Run(() => ProcessQueuedVersionAsync(connector, targetDirectory, filesToLoad));
    }

    private async Task ProcessQueuedVersionAsync(
        ConnectorDescriptor connector,
        string targetDirectory,
        IReadOnlyCollection<QueuedVersionFile> queuedFiles)
    {
        try
        {
            Directory.CreateDirectory(targetDirectory);
            var connectorFiles = new List<ContentVersionExecuteDto>(queuedFiles.Count);
            foreach (var file in queuedFiles)
            {
                var bytes = await _apiService.ContentFiles.DownloadAsync(file.ContentFileId);
                await File.WriteAllBytesAsync(file.Path, bytes);
                connectorFiles.Add(new ContentVersionExecuteDto(
                    file.LoadingId,
                    file.FileName,
                    file.Path));
            }

            var response = await _connectorClient.LoadContentVersionAsync(
                connector,
                connectorFiles);
            if (!response.Success)
            {
                throw new InvalidOperationException("Connector не подтвердил загрузку версии.");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or
                                             IOException or System.Net.Sockets.SocketException or UnauthorizedAccessException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Фоновая загрузка версии не выполнена: {exception}");
        }
    }

    private static string CreateSafeDirectoryName(string value, string fallback)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Trim()
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }

    private static string GetVersionFilePath(
        string directory,
        string fileName,
        int fileId,
        bool includeFileId)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = $"file-{fileId}";
        }

        if (includeFileId)
        {
            safeName = $"{Path.GetFileNameWithoutExtension(safeName)}-{fileId}{Path.GetExtension(safeName)}";
        }

        return Path.Combine(directory, safeName);
    }

}

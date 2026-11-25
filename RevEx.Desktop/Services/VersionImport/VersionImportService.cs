using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;
using RevEx.Domain.Identifiers;

namespace RevEx.Desktop.Services.VersionImport;

public sealed class VersionImportService
{
    private const long MaximumFileSizeBytes = 100 * 1024 * 1024;
    private readonly IRevExApiService _api;
    private readonly RfaFamilyImportService _revitImport;

    public VersionImportService(IRevExApiService api, RfaFamilyImportService revitImport)
    {
        _api = api;
        _revitImport = revitImport;
    }

    public async Task<VersionImportSession> ReadAsync(
        ContentDto content,
        Platform platform,
        IVersionFileConnector connector,
        IReadOnlyCollection<string> paths,
        CancellationToken cancellationToken = default)
    {
        var selectedPaths = ValidateFiles(paths);
        if (!connector.SupportsPlatform(platform))
        {
            throw new InvalidOperationException($"Коннектор «{connector.Name}» не поддерживает выбранную платформу.");
        }

        if (platform == Platform.Revit && connector.RemoteDescriptor is not null)
        {
            var primaryFiles = selectedPaths.Where(path =>
                string.Equals(Path.GetExtension(path), ".rfa", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (primaryFiles.Length != 1)
            {
                throw new InvalidOperationException("Для Revit выберите один RFA-файл.");
            }

            var auxiliaryFiles = selectedPaths.Except(primaryFiles, StringComparer.OrdinalIgnoreCase).ToArray();
            var revitSession = await _revitImport.ReadAsync(
                primaryFiles[0],
                connector.RemoteDescriptor,
                auxiliaryFiles,
                content);
            var file = new ContentVersionExecuteDto(
                Guid.NewGuid(),
                revitSession.FileName,
                revitSession.SourceFilePath);
            var metadata = new ConnectorFileMetadata(
                file,
                revitSession.Metadata.ToString(Formatting.None));
            return new VersionImportSession(content, platform, connector, [metadata], revitSession);
        }

        var files = selectedPaths
            .Select(path => new ContentVersionExecuteDto(Guid.NewGuid(), Path.GetFileName(path), path))
            .ToArray();
        var metadataFiles = await connector.ReadAsync(files, cancellationToken);
        ValidateResponse(files, metadataFiles);
        return new VersionImportSession(content, platform, connector, metadataFiles);
    }

    public async Task<ContentVersionDto> SaveAsync(
        VersionImportSession session,
        string versionName,
        string applicationVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(versionName))
        {
            throw new InvalidOperationException("Укажите название версии.");
        }

        if (session.RevitSession is not null)
        {
            return await _revitImport.SaveAsync(
                session.RevitSession,
                session.Content.Description,
                session.Content.CategoryId,
                versionName,
                applicationVersion);
        }

        if (!Uuid7.IsValid(session.Content.Cid))
        {
            throw new InvalidOperationException("CID текущего контента отсутствует или не является UUID v7.");
        }

        var vid = session.Vid ??= await _api.Contents.GenerateUuid7Async(cancellationToken);
        var sourceFiles = session.Files.Select(file => file.File).ToArray();
        if (session.PreparedFiles is null)
        {
            var preparedFiles = await session.Connector.PrepareAsync(
                session.Content.Cid, vid, session.Platform, sourceFiles, cancellationToken);
            ValidatePreparedFiles(sourceFiles, preparedFiles);
            session.PreparedFiles = preparedFiles;
        }

        var uploads = sourceFiles.Select(source =>
        {
            var prepared = session.PreparedFiles.Single(file => file.Id == source.Id);
            var metadata = session.Files.Single(file => file.File.Id == source.Id).MetadataJson;
            return new VersionImportAuxiliaryFile(prepared.Path, prepared.Name, metadata);
        }).ToArray();
        var primary = uploads[0];
        var request = new VersionImportRequest(
            session.Content.Cid,
            vid,
            session.Content.Name,
            null,
            null,
            versionName.Trim(),
            DateTimeOffset.UtcNow,
            string.IsNullOrWhiteSpace(applicationVersion) ? session.SuggestedApplicationVersion : applicationVersion.Trim(),
            primary.Path,
            primary.Name,
            primary.MetadataJson!,
            uploads.Skip(1).ToArray(),
            session.Platform,
            GetApplicationName(session));
        return await _api.VersionImports.ImportAsync(request, cancellationToken);
    }

    private static string[] ValidateFiles(IReadOnlyCollection<string> paths)
    {
        var selectedPaths = paths
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (selectedPaths.Length == 0)
        {
            throw new InvalidOperationException("Выберите хотя бы один файл.");
        }

        foreach (var path in selectedPaths)
        {
            var file = new FileInfo(path);
            if (!file.Exists)
            {
                throw new FileNotFoundException($"Файл «{file.Name}» не найден.", path);
            }

            if (file.Length == 0 || file.Length > MaximumFileSizeBytes)
            {
                throw new InvalidOperationException($"Размер файла «{file.Name}» должен быть от 1 байта до 100 МБ.");
            }
        }

        return selectedPaths;
    }

    private static void ValidateResponse(
        IReadOnlyCollection<ContentVersionExecuteDto> requested,
        IReadOnlyCollection<ConnectorFileMetadata> returned)
    {
        var requestedIds = requested.Select(file => file.Id).ToHashSet();
        if (returned.Count != requested.Count || returned.Any(file =>
                !requestedIds.Contains(file.File.Id) || string.IsNullOrWhiteSpace(file.MetadataJson)))
        {
            throw new InvalidOperationException("Коннектор вернул неполный набор метаданных или данные другого файла.");
        }
    }

    private static void ValidatePreparedFiles(
        IReadOnlyCollection<ContentVersionExecuteDto> requested,
        IReadOnlyCollection<ContentVersionExecuteDto> prepared)
    {
        var requestedIds = requested.Select(file => file.Id).ToHashSet();
        if (prepared.Select(file => file.Id).Distinct().Count() != requested.Count ||
            prepared.Count != requested.Count || prepared.Any(file =>
                !requestedIds.Contains(file.Id) ||
                string.IsNullOrWhiteSpace(file.Name) ||
                !Path.IsPathRooted(file.Path) ||
                !File.Exists(file.Path)))
        {
            throw new InvalidOperationException("Коннектор вернул неполный или недоступный набор подготовленных файлов.");
        }
    }

    private static string GetApplicationName(VersionImportSession session)
    {
        return session.Connector.RemoteDescriptor?.Product.ToString().ToLowerInvariant()
            ?? (session.Platform == Platform.AutoCad ? "autocad" : "file");
    }

}

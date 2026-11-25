using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevEx.Connector.Client;
using RevEx.Connector.Contracts;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Contracts.Revit;
using RevEx.Domain.Entities;
using RevEx.Domain.Identifiers;

namespace RevEx.Desktop.Services;

public sealed class RfaFamilyImportService
{
    private readonly IRevExApiService _api;
    private readonly IConnectorClient _client;
    private readonly IConnectorRegistry _registry;
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    public RfaFamilyImportService(IRevExApiService api, IConnectorClient client, IConnectorRegistry registry)
    {
        _api = api;
        _client = client;
        _registry = registry;
    }

    public event EventHandler? ConnectorsChanged
    {
        add
        {
            _registry.Changed += value;
        }
        remove
        {
            _registry.Changed -= value;
        }
    }

    public IReadOnlyCollection<ConnectorDescriptor> GetAvailableConnectors()
    {
        return _registry.GetAll();
    }

    public async Task<RfaFamilyReadSession> ReadAsync(
        string path,
        ConnectorDescriptor connector,
        IReadOnlyCollection<string>? auxiliaryPaths = null,
        ContentDto? targetContent = null)
    {
        if (!string.Equals(Path.GetExtension(path), ".rfa", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            throw new InvalidOperationException("Выберите существующий файл семейства (*.rfa).");
        }

        if (targetContent is not null && !Uuid7.IsValid(targetContent.Cid))
        {
            throw new InvalidOperationException("CID выбранного контента отсутствует или не является UUID v7.");
        }

        var sourceFilePath = Path.GetFullPath(path);
        var fileInfo = new FileInfo(sourceFilePath);
        if (fileInfo.Length == 0 || fileInfo.Length > 100 * 1024 * 1024)
        {
            throw new InvalidOperationException("Размер семейства должен быть от 1 байта до 100 МБ.");
        }

        var auxiliaryFiles = (auxiliaryPaths ?? []).Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(filePath => new SelectedLocalFile(filePath, Path.GetFileName(filePath)))
            .ToArray();
        if (auxiliaryFiles.GroupBy(file => file.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("Вспомогательные файлы должны иметь разные имена.");
        }

        foreach (var auxiliaryFile in auxiliaryFiles)
        {
            var extension = Path.GetExtension(auxiliaryFile.Path);
            if ((!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(extension, ".ifc", StringComparison.OrdinalIgnoreCase)) || !File.Exists(auxiliaryFile.Path))
            {
                throw new InvalidOperationException("В качестве вспомогательных файлов выберите существующие CSV или IFC.");
            }

            var auxiliaryInfo = new FileInfo(auxiliaryFile.Path);
            if (auxiliaryInfo.Length == 0 || auxiliaryInfo.Length > 100 * 1024 * 1024)
            {
                throw new InvalidOperationException($"Размер файла «{auxiliaryFile.Name}» должен быть от 1 байта до 100 МБ.");
            }
        }

        ValidateReader(connector);
        var directory = Path.Combine(Path.GetTempPath(), "RevEx", "RfaImport", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var session = new RfaFamilyReadSession(
            directory,
            sourceFilePath,
            Path.GetFileName(sourceFilePath),
            connector);
        try
        {
            // Revit reads a working copy so another process can open the selected family safely.
            await using (var source = File.OpenRead(session.SourceFilePath))
            await using (var destination = File.Create(session.FilePath))
            {
                await source.CopyToAsync(destination);
            }

            var copiedFiles = new List<SelectedLocalFile>();
            foreach (var auxiliaryFile in auxiliaryFiles)
            {
                var copyPath = Path.Combine(directory, auxiliaryFile.Name);
                await using var source = File.OpenRead(auxiliaryFile.Path);
                await using var destination = File.Create(copyPath);
                await source.CopyToAsync(destination);
                copiedFiles.Add(new SelectedLocalFile(copyPath, auxiliaryFile.Name));
            }

            session.AuxiliaryFiles = copiedFiles;

            var file = new ContentVersionExecuteDto(Guid.NewGuid(), session.FileName, session.FilePath);
            var task = new ConnectorTask(Guid.NewGuid(), $"Чтение {session.FileName}", ConnectorCommand.ReadFiles);
            var response = await _client.ExecuteAsync<ConnecterReadFileRequest, ConnectorReadFilesResponse>(
                connector, task, new ConnecterReadFileRequest([file]));
            if (response.Files.Count != 1)
            {
                throw new InvalidOperationException("Коннектор не вернул метаданные выбранного семейства.");
            }

            var result = response.Files.Single();
            if (result.File.Id != file.Id || string.IsNullOrWhiteSpace(result.MetadataJson))
            {
                throw new InvalidOperationException("Коннектор вернул метаданные другого файла.");
            }

            var metadata = JObject.Parse(result.MetadataJson);

            // Accept older connectors, but persist the unambiguous Revit field name.
            var legacyCategory = metadata.Property("Category", StringComparison.OrdinalIgnoreCase);
            if (legacyCategory is not null)
            {
                if (metadata.GetValue("RevitCategory", StringComparison.OrdinalIgnoreCase) is null)
                {
                    metadata["RevitCategory"] = legacyCategory.Value.DeepClone();
                }

                legacyCategory.Remove();
            }

            // Preserve fields unknown to the current Revit DTO.
            session.Metadata = (JObject)metadata.DeepClone();
            session.Family = metadata.ToObject<RevitFamilyReadResult>()
                ?? throw new InvalidOperationException("Не удалось прочитать ответ коннектора.");
            var fileCid = ParseIdentifier(session.Family.CID?.Value);
            session.SavedContent = targetContent ?? (fileCid is Guid cid
                ? await FindContentAsync(cid)
                : null);
            session.Cid = session.SavedContent?.Cid ?? await _api.Contents.GenerateUuid7Async();
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public Task<ContentDto?> FindContentAsync(Guid cid)
    {
        return _api.Contents.GetByCidAsync(cid);
    }

    public async Task<ContentVersionDto> SaveAsync(
        RfaFamilyReadSession session, string description, int? categoryId, string versionName, string applicationVersion)
    {
        await _saveGate.WaitAsync();
        try
        {
            await WriteIdentifiersAsync(session);
            var request = new VersionImportRequest(
                session.Cid,
                session.Vid!.Value,
                session.ContentName,
                string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                categoryId,
                versionName.Trim(),
                DateTimeOffset.UtcNow,
                applicationVersion,
                session.SourceFilePath,
                session.FileName,
                session.Metadata.ToString(Formatting.None),
                session.AuxiliaryFiles
                    .Select(file => new VersionImportAuxiliaryFile(file.Path, file.Name))
                    .ToArray(),
                Platform.Revit,
                "revit");
            session.SavedVersion = await _api.VersionImports.ImportAsync(request);
            return session.SavedVersion;
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private void ValidateReader(ConnectorDescriptor connector)
    {
        if (!_registry.GetAll().Any(item => item.InstanceId == connector.InstanceId))
        {
            throw new InvalidOperationException("Выбранный коннектор больше недоступен. Выберите другой коннектор.");
        }

        if (!connector.Commands.Any(command => command.Name == ConnectorCommand.ReadFiles))
        {
            throw new InvalidOperationException($"Коннектор «{connector.Name}» не поддерживает чтение RFA. Выберите другой коннектор.");
        }

        if (!connector.Commands.Any(command => command.Name == ConnectorCommand.PrepareContentVersion))
        {
            throw new InvalidOperationException($"Коннектор «{connector.Name}» не поддерживает подготовку файла версии. Выберите другой коннектор.");
        }
    }

    private async Task WriteIdentifiersAsync(RfaFamilyReadSession session)
    {
        if (session.AreIdentifiersWritten)
        {
            return;
        }

        ValidateReader(session.Connector);
        session.Vid ??= await _api.Contents.GenerateUuid7Async();
        var file = new ContentVersionExecuteDto(Guid.NewGuid(), session.FileName, session.SourceFilePath);
        var request = new PrepareContentVersionRequest(
            session.Cid,
            session.Vid.Value,
            Platform.Revit,
            [file]);
        var task = new ConnectorTask(
            Guid.NewGuid(),
            $"Подготовка {session.FileName}",
            ConnectorCommand.PrepareContentVersion);
        var response = await _client.ExecuteAsync<PrepareContentVersionRequest, PrepareContentVersionResponse>(
            session.Connector,
            task,
            request);
        var writtenCid = response.Cid;
        var writtenVid = response.Vid;

        if (writtenCid != session.Cid || writtenVid != session.Vid.Value)
        {
            throw new InvalidOperationException("Коннектор вернул CID или VID, отличающийся от записанного значения.");
        }

        UpdateIdentifierMetadata(session, writtenCid, writtenVid);
        session.AreIdentifiersWritten = true;
    }

    private static Guid? ParseIdentifier(string? value)
    {
        if (!Guid.TryParse(value, out var identifier) || !Uuid7.IsValid(identifier))
        {
            return null;
        }

        return identifier;
    }

    private static void UpdateIdentifierMetadata(RfaFamilyReadSession session, Guid cid, Guid vid)
    {
        var cidParameter = CreateIdentifierParameter("RevEx_CID", cid);
        var vidParameter = CreateIdentifierParameter("RevEx_VID", vid);
        session.Family.CID = cidParameter;
        session.Family.VID = vidParameter;

        session.Metadata["CID"] = JObject.FromObject(cidParameter);
        session.Metadata["VID"] = JObject.FromObject(vidParameter);
        if (session.Metadata["Types"] is not JArray types)
        {
            return;
        }

        foreach (var type in types.OfType<JObject>())
        {
            if (type["Parameters"] is not JArray parameters)
            {
                continue;
            }

            ReplaceIdentifierParameter(parameters, cidParameter);
            ReplaceIdentifierParameter(parameters, vidParameter);
        }
    }

    private static RevitFamilyReadResultTypeParameter CreateIdentifierParameter(string name, Guid value)
    {
        return new RevitFamilyReadResultTypeParameter
        {
            Name = name,
            Value = value.ToString("D"),
            StorageType = "String",
            GroupId = "PG_IDENTITY_DATA",
            GroupName = "Данные идентификации"
        };
    }

    private static void ReplaceIdentifierParameter(
        JArray parameters,
        RevitFamilyReadResultTypeParameter parameter)
    {
        var existing = parameters.OfType<JObject>().FirstOrDefault(item =>
            string.Equals(
                item.GetValue("Name", StringComparison.OrdinalIgnoreCase)?.Value<string>(),
                parameter.Name,
                StringComparison.Ordinal));
        var value = JObject.FromObject(parameter);
        if (existing is null)
        {
            parameters.Add(value);
            return;
        }

        existing.Replace(value);
    }
}

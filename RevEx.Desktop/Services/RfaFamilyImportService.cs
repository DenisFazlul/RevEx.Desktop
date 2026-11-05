using System;
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

    public async Task<RfaFamilyReadSession> ReadAsync(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".rfa", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            throw new InvalidOperationException("Выберите существующий файл семейства (*.rfa).");
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length == 0 || fileInfo.Length > 100 * 1024 * 1024)
        {
            throw new InvalidOperationException("Размер семейства должен быть от 1 байта до 100 МБ.");
        }

        var connector = await FindReaderAsync();
        var directory = Path.Combine(Path.GetTempPath(), "RevEx", "RfaImport", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var session = new RfaFamilyReadSession(directory, Path.GetFileName(path), connector);
        try
        {
            // Revit may prepare and save the document. Work on a copy and upload that same copy.
            await using (var source = File.OpenRead(path))
            await using (var destination = File.Create(session.FilePath))
            {
                await source.CopyToAsync(destination);
            }

            var file = new ContentVersionExecuteDto(Guid.NewGuid(), session.FileName, session.FilePath);
            var task = new ConnectorTask(Guid.NewGuid(), $"Чтение {session.FileName}", ConnectorCommand.ReadFiles);
            var response = await _client.ExecuteAsync<ConnecterReadFileRequest, JObject>(
                connector, task, new ConnecterReadFileRequest([file]));
            var results = response.GetValue("FamilyReadResults", StringComparison.OrdinalIgnoreCase) as JArray;
            if (results is null || results.Count != 1 || results[0] is not JObject metadata)
            {
                throw new InvalidOperationException("Коннектор не вернул метаданные выбранного семейства.");
            }

            // Preserve the original JSON, including fields unknown to the current Revit DTO.
            session.Metadata = (JObject)metadata.DeepClone();
            session.Family = metadata.ToObject<RevitFamilyReadResult>()
                ?? throw new InvalidOperationException("Не удалось прочитать ответ коннектора.");
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    public async Task<ContentDto?> FindContentAsync(string name)
    {
        var contents = await _api.Contents.GetAllAsync(new ContentQueryDto { Name = name });
        var matches = contents.Where(content =>
            string.Equals(content.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length > 1)
        {
            throw new InvalidOperationException($"Найдено несколько карточек «{name}». Устраните совпадение имён перед добавлением версии.");
        }

        return matches.SingleOrDefault();
    }

    public async Task<ContentVersionDto> SaveAsync(
        RfaFamilyReadSession session, string description, int? categoryId, string versionName, string applicationVersion)
    {
        await _saveGate.WaitAsync();
        try
        {
            // Recheck immediately before saving: the card may have appeared while the form was open.
            session.SavedContent ??= await FindContentAsync(session.ContentName);
            if (session.SavedContent is null)
            {
                if (categoryId is null || string.IsNullOrWhiteSpace(description))
                {
                    throw new InvalidOperationException("Для нового контента укажите описание и категорию.");
                }

                session.SavedContent = await _api.Contents.CreateAsync(new CreateContentDto
                {
                    Name = session.ContentName,
                    Description = description.Trim(),
                    CategoryId = categoryId.Value
                });
            }

            session.SavedVersion ??= await _api.ContentVersions.CreateAsync(new CreateContentVersionDto
            {
                ContentId = session.SavedContent.Id,
                Name = versionName.Trim(),
                Date = DateTimeOffset.UtcNow,
                Status = "draft",
                Application = "revit",
                ApplicationVersion = applicationVersion
            });

            if (!session.IsFileUploaded)
            {
                // A retry after an interrupted upload must reuse the version already created.
                var current = await _api.ContentVersions.GetByIdAsync(session.SavedVersion.Id)
                    ?? throw new InvalidOperationException("Созданная версия больше не существует.");
                foreach (var fileId in current.FileIds)
                {
                    var file = await _api.ContentFiles.GetByIdAsync(fileId);
                    if (file?.Role == "primary")
                    {
                        session.SavedVersion = current;
                        session.IsFileUploaded = true;
                        break;
                    }
                }

                if (!session.IsFileUploaded)
                {
                    await using var stream = File.OpenRead(session.FilePath);
                    session.SavedVersion = await _api.ContentVersions.UploadFileAsync(
                        current.Id, stream, session.FileName, "primary",
                        metadataJson: session.Metadata.ToString(Formatting.None));
                    session.IsFileUploaded = true;
                }
            }

            return session.SavedVersion;
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private async Task<ConnectorDescriptor> FindReaderAsync()
    {
        var candidates = _registry.GetAll()
            .Where(connector => connector.Product is ConnectorProduct.Revit or ConnectorProduct.Mock)
            .OrderBy(connector => connector.Product == ConnectorProduct.Mock)
            .ThenByDescending(connector => connector.ProductVersion, StringComparer.OrdinalIgnoreCase)
            .ThenBy(connector => connector.InstanceId);
        foreach (var connector in candidates)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var task = new ConnectorTask(Guid.NewGuid(), "Проверка чтения RFA", ConnectorCommand.GetCommands);
                var commands = await _client.ExecuteAsync<object, ConnectorCommand[]>(
                    connector, task, new object(), timeout.Token);
                if (commands.Contains(ConnectorCommand.ReadFiles))
                {
                    return connector;
                }
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or OperationCanceledException)
            {
                // Continue searching if a registration has become unavailable.
            }
        }

        throw new InvalidOperationException("Не найден доступный коннектор чтения RFA. Запустите коннектор Revit или мок-коннектор с командой ReadFiles и повторите выбор файла.");
    }
}

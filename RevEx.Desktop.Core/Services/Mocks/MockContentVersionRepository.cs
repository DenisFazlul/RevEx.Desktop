using RevEx.Domain.Contracts.ContentFile;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Domain.Entities;
using static RevEx.Desktop.Core.Services.MockApiData;

namespace RevEx.Desktop.Core.Services;

internal sealed class MockContentVersionRepository : IContentVersionRepository
{
    public Task<IReadOnlyCollection<ContentVersionDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        FromCollection(ContentVersions, cancellationToken);

    public Task<ContentVersionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(ContentVersions.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<ContentVersionDto> CreateAsync(
        CreateContentVersionDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var platform = request.Platform;
        if (platform is null || !Enum.IsDefined(typeof(Platform), platform.Value)
            || !Enum.IsDefined(typeof(PublishStatus), request.PublishStatus))
        {
            throw new InvalidOperationException("Укажите допустимые платформу и статус публикации версии.");
        }

        var version = new ContentVersionDto(
            ContentVersions.Count == 0 ? 1 : ContentVersions.Max(item => item.Id) + 1,
            request.ContentId,
            [],
            request.Name ?? "Новая версия",
            request.Date ?? DateTimeOffset.UtcNow,
            GetLegacyStatus(request.PublishStatus),
            request.PublishStatus,
            platform.Value,
            platform.Value switch
            {
                Platform.Revit => "revit",
                Platform.AutoCad => "autocad",
                _ => request.Application
            },
            request.ApplicationVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        ContentVersions.Add(version);
        return Task.FromResult(version);
    }

    public Task UpdatePublishStatusAsync(
        int versionId, PublishStatus publishStatus, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Enum.IsDefined(typeof(PublishStatus), publishStatus))
        {
            throw new InvalidOperationException("Укажите допустимый статус публикации.");
        }

        var index = ContentVersions.FindIndex(version => version.Id == versionId);
        if (index < 0)
        {
            throw new HttpRequestException($"Version {versionId} was not found.");
        }

        ContentVersions[index] = ContentVersions[index] with
        {
            PublishStatus = publishStatus,
            Status = GetLegacyStatus(publishStatus),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.CompletedTask;
    }

    private static string GetLegacyStatus(PublishStatus status)
    {
        return status switch
        {
            PublishStatus.Ready => "published",
            PublishStatus.Withdrawn => "archived",
            _ => "draft"
        };
    }

    public Task<ContentVersionDto> UploadFileAsync(
        int versionId,
        Stream content,
        string fileName,
        string role,
        CancellationToken cancellationToken = default,
        string? metadataJson = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var versionIndex = ContentVersions.FindIndex(item => item.Id == versionId);
        if (versionIndex < 0)
            throw new HttpRequestException($"Version {versionId} was not found.");

        var fileId = ContentFiles.Count == 0 ? 1 : ContentFiles.Max(item => item.Id) + 1;
        ContentFiles.Add(new ContentFileDto(
            fileId,
            $"/api/content-files/{fileId}/content",
            versionId,
            fileName,
            Path.GetExtension(fileName),
            content.CanSeek ? content.Length : 0,
            role,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow)
        {
            Metadata = metadataJson is null ? null : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(metadataJson)
        });

        var version = ContentVersions[versionIndex];
        version = version with { FileIds = version.FileIds.Append(fileId).ToArray() };
        ContentVersions[versionIndex] = version;
        return Task.FromResult(version);
    }

    public Task DeleteFileAsync(
        int versionId,
        int fileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = ContentVersions.FindIndex(item => item.Id == versionId);
        if (index < 0 || ContentFiles.All(item => item.Id != fileId || item.ContentVersionId != versionId))
            throw new HttpRequestException($"File {fileId} was not found in version {versionId}.");

        ContentFiles.RemoveAll(item => item.Id == fileId);
        ContentVersions[index] = ContentVersions[index] with
        {
            FileIds = ContentVersions[index].FileIds.Where(id => id != fileId).ToArray(),
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.CompletedTask;
    }
}

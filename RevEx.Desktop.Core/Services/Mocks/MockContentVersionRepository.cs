using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
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
        var version = new ContentVersionDto(
            ContentVersions.Count == 0 ? 1 : ContentVersions.Max(item => item.Id) + 1,
            request.ContentId,
            [],
            request.Name ?? "Новая версия",
            request.Date ?? DateTimeOffset.UtcNow,
            request.Status ?? "draft",
            request.Application,
            request.ApplicationVersion,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        ContentVersions.Add(version);
        return Task.FromResult(version);
    }

    public Task<ContentVersionDto> UploadFileAsync(
        int versionId,
        Stream content,
        string fileName,
        string role,
        CancellationToken cancellationToken = default)
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
            DateTimeOffset.UtcNow));

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

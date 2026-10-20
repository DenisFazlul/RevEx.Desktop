using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using static RevEx.Desktop.Core.Services.MockApiData;

namespace RevEx.Desktop.Core.Services;

internal sealed class MockContentRepository : IContentRepository
{
    public Task<IReadOnlyCollection<ContentDto>> GetAllAsync(
        ContentQueryDto query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var categoryIds = query.CategoryIds.ToHashSet();
        var result = Contents.AsEnumerable();

        if (categoryIds.Count > 0)
        {
            result = result.Where(content => categoryIds.Contains(content.CategoryId));
        }

        var tagIds = query.TagIds.ToHashSet();
        if (tagIds.Count > 0)
        {
            result = result.Where(content =>
                tagIds.All(tagId => content.TagIds.Contains(tagId)));
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            result = result.Where(content =>
                content.Name.Contains(query.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyCollection<ContentDto>>(result.ToArray());
    }

    public Task<ContentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Contents.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<ContentDto> CreateAsync(
        CreateContentDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var content = new ContentDto(
            Contents.Count == 0 ? 1 : Contents.Max(item => item.Id) + 1,
            request.Name,
            request.Description,
            request.CategoryId,
            request.ContentStatusId ?? 1,
            null,
            request.TagIds?.ToArray() ?? [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        Contents.Add(content);
        return Task.FromResult(content);
    }

    public Task UpdateAsync(
        int id,
        UpdateContentDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = Contents.FindIndex(item => item.Id == id);
        if (index < 0)
            throw new HttpRequestException($"Content {id} was not found.");

        var current = Contents[index];
        Contents[index] = current with
        {
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            ContentStatusId = request.ContentStatusId ?? current.ContentStatusId,
            TagIds = request.TagIds?.ToArray() ?? current.TagIds,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Contents.RemoveAll(item => item.Id == id);
        ContentVersions.RemoveAll(item => item.ContentId == id);
        return Task.CompletedTask;
    }

    public Task<ContentDto> UploadPreviewAsync(
        int id,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = Contents.FindIndex(item => item.Id == id);
        if (index < 0)
            throw new HttpRequestException($"Content {id} was not found.");

        var fileId = ContentFiles.Count == 0 ? 1 : ContentFiles.Max(item => item.Id) + 1;
        ContentFiles.Add(new ContentFileDto(
            fileId, $"/api/content-files/{fileId}/content", null, fileName,
            Path.GetExtension(fileName), content.CanSeek ? content.Length : 0, "preview",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var updated = Contents[index] with { PreviewFileId = fileId, UpdatedAt = DateTimeOffset.UtcNow };
        Contents[index] = updated;
        return Task.FromResult(updated);
    }
}

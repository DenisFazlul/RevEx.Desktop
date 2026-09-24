using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class MockRevExApiService : IRevExApiService
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 1, 15, 12, 30, 0, TimeSpan.Zero);

    private static readonly CategoryDto[] Categories =
    [
        new(1, "Общие", CreatedAt, UpdatedAt),
        new(2, "Документы", CreatedAt, UpdatedAt)
    ];

    private static readonly List<TagGroupDto> TagGroups =
    [
        new(1, "Проекты", CreatedAt, UpdatedAt),
        new(2, "Назначение", CreatedAt, UpdatedAt)
    ];

    private static readonly List<TagDto> Tags =
    [
        new(1, "Важное", 2, "Назначение", CreatedAt, UpdatedAt),
        new(2, "Работа", 1, "Проекты", CreatedAt, UpdatedAt)
    ];

    private static readonly ContentStatusDto[] ContentStatuses =
    [
        new(1, "Черновик", CreatedAt, UpdatedAt),
        new(2, "Опубликовано", CreatedAt, UpdatedAt)
    ];

    private static readonly List<ContentFileDto> ContentFiles =
    [
        new(1, "/api/content-files/1/content", 1, "Example.rfa", ".rfa", 1024, "primary", CreatedAt, UpdatedAt),
        new(2, "/api/content-files/2/content", null, "Preview.png", ".png", 512, "preview", CreatedAt, UpdatedAt)
    ];

    private static readonly List<ContentVersionDto> ContentVersions =
    [
        new(1, 1, [1], "Версия 1.0", CreatedAt, "published", "revit", "2026", CreatedAt, UpdatedAt)
    ];

    private static readonly List<ContentDto> Contents =
    [
        new(1, "Пример материала", "Тестовый материал из мок-сервиса.", 1, 2, 2, [1, 2], CreatedAt, UpdatedAt),
        new(2, "Рабочий документ", "Материал без превью.", 2, 1, null, [2], CreatedAt, UpdatedAt)
    ];

    public Task<IReadOnlyCollection<ContentDto>> GetContentsAsync(
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

    public Task<ContentDto?> GetContentAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Contents.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<ContentDto> CreateContentAsync(
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

    public Task UpdateContentAsync(
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

    public Task<ContentDto> UploadContentPreviewAsync(
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

    public Task<byte[]> DownloadContentFileAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ContentFiles.All(item => item.Id != id))
            throw new HttpRequestException($"File {id} was not found.");

        return Task.FromResult(Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
    }

    public Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        FromCollection(Categories, cancellationToken);

    public Task<CategoryDto?> GetCategoryAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Categories.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<IReadOnlyCollection<TagGroupDto>> GetTagGroupsAsync(CancellationToken cancellationToken = default) =>
        FromCollection(TagGroups, cancellationToken);

    public Task<TagGroupDto> CreateTagGroupAsync(CreateTagGroupDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var group = new TagGroupDto(TagGroups.Count == 0 ? 1 : TagGroups.Max(item => item.Id) + 1,
            request.Name, now, now);
        TagGroups.Add(group);
        return Task.FromResult(group);
    }

    public Task UpdateTagGroupAsync(int id, UpdateTagGroupDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = TagGroups.FindIndex(item => item.Id == id);
        if (index < 0) throw new HttpRequestException($"Tag group {id} was not found.");
        TagGroups[index] = TagGroups[index] with { Name = request.Name, UpdatedAt = DateTimeOffset.UtcNow };
        for (var tagIndex = 0; tagIndex < Tags.Count; tagIndex++)
            if (Tags[tagIndex].TagGroupId == id)
                Tags[tagIndex] = Tags[tagIndex] with { TagGroupName = request.Name };
        return Task.CompletedTask;
    }

    public Task DeleteTagGroupAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Tags.Any(item => item.TagGroupId == id))
            throw new HttpRequestException("Move or delete all tags in this group first.");
        TagGroups.RemoveAll(item => item.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default) =>
        FromCollection(Tags, cancellationToken);

    public Task<TagDto?> GetTagAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Tags.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<TagDto> CreateTagAsync(
        CreateTagDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var group = TagGroups.FirstOrDefault(item => item.Id == request.TagGroupId)
            ?? throw new HttpRequestException($"Tag group {request.TagGroupId} was not found.");
        var tag = new TagDto(Tags.Count == 0 ? 1 : Tags.Max(item => item.Id) + 1,
            request.Name, group.Id, group.Name, now, now);
        Tags.Add(tag);
        return Task.FromResult(tag);
    }

    public Task UpdateTagAsync(
        int id,
        UpdateTagDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = Tags.FindIndex(item => item.Id == id);
        if (index < 0)
            throw new HttpRequestException($"Tag {id} was not found.");

        var group = TagGroups.FirstOrDefault(item => item.Id == request.TagGroupId)
            ?? throw new HttpRequestException($"Tag group {request.TagGroupId} was not found.");
        Tags[index] = Tags[index] with
        {
            Name = request.Name,
            TagGroupId = group.Id,
            TagGroupName = group.Name,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.CompletedTask;
    }

    public Task DeleteTagAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Tags.RemoveAll(item => item.Id == id);
        for (var index = 0; index < Contents.Count; index++)
        {
            var content = Contents[index];
            Contents[index] = content with
            {
                TagIds = content.TagIds.Where(tagId => tagId != id).ToArray(),
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ContentStatusDto>> GetContentStatusesAsync(
        CancellationToken cancellationToken = default) =>
        FromCollection(ContentStatuses, cancellationToken);

    public Task<ContentStatusDto?> GetContentStatusAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(ContentStatuses.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<IReadOnlyCollection<ContentVersionDto>> GetContentVersionsAsync(
        CancellationToken cancellationToken = default) =>
        FromCollection(ContentVersions, cancellationToken);

    public Task<ContentVersionDto?> GetContentVersionAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(ContentVersions.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<ContentVersionDto> CreateContentVersionAsync(
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

    public Task<ContentVersionDto> UploadContentVersionFileAsync(
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

    public Task<IReadOnlyCollection<ContentFileDto>> GetContentFilesAsync(
        CancellationToken cancellationToken = default) =>
        FromCollection(ContentFiles, cancellationToken);

    public Task<ContentFileDto?> GetContentFileAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(ContentFiles.FirstOrDefault(item => item.Id == id), cancellationToken);

    private static Task<IReadOnlyCollection<T>> FromCollection<T>(
        IEnumerable<T> items,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<T>>(items.ToArray());
    }

    private static Task<T?> FromItem<T>(T? item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(item);
    }
}

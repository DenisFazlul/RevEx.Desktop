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

    private static readonly TagDto[] Tags =
    [
        new(1, "Важное", CreatedAt, UpdatedAt),
        new(2, "Работа", CreatedAt, UpdatedAt)
    ];

    private static readonly ContentStatusDto[] ContentStatuses =
    [
        new(1, "Черновик", CreatedAt, UpdatedAt),
        new(2, "Опубликовано", CreatedAt, UpdatedAt)
    ];

    private static readonly ContentFileDto[] ContentFiles =
    [
        new(1, "/api/content-files/1/content", 1, CreatedAt, UpdatedAt),
        new(2, "/api/content-files/2/content", null, CreatedAt, UpdatedAt)
    ];

    private static readonly ContentVersionDto[] ContentVersions =
    [
        new(1, 1, [1], "Версия 1.0", CreatedAt, "published", CreatedAt, UpdatedAt)
    ];

    private static readonly ContentDto[] Contents =
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

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            result = result.Where(content =>
                content.Name.Contains(query.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyCollection<ContentDto>>(result.ToArray());
    }

    public Task<ContentDto?> GetContentAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Contents.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<IReadOnlyCollection<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        FromCollection(Categories, cancellationToken);

    public Task<CategoryDto?> GetCategoryAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Categories.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<IReadOnlyCollection<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default) =>
        FromCollection(Tags, cancellationToken);

    public Task<TagDto?> GetTagAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Tags.FirstOrDefault(item => item.Id == id), cancellationToken);

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

    public Task<IReadOnlyCollection<ContentFileDto>> GetContentFilesAsync(
        CancellationToken cancellationToken = default) =>
        FromCollection(ContentFiles, cancellationToken);

    public Task<ContentFileDto?> GetContentFileAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(ContentFiles.FirstOrDefault(item => item.Id == id), cancellationToken);

    private static Task<IReadOnlyCollection<T>> FromCollection<T>(
        T[] items,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<T>>(items);
    }

    private static Task<T?> FromItem<T>(T? item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(item);
    }
}

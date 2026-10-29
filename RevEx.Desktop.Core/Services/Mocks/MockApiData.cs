using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentFile;
using RevEx.Domain.Contracts.ContentHistory;
using RevEx.Domain.Contracts.ContentStatus;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.Core.Services;

internal static class MockApiData
{
    internal static readonly DateTimeOffset CreatedAt = new(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);
    internal static readonly DateTimeOffset UpdatedAt = new(2026, 1, 15, 12, 30, 0, TimeSpan.Zero);

    internal static readonly List<CategoryDto> Categories =
    [
        new(1, "Общие", CreatedAt, UpdatedAt),
        new(2, "Документы", CreatedAt, UpdatedAt)
    ];

    internal static readonly List<TagGroupDto> TagGroups =
    [
        new(1, "Проекты", CreatedAt, UpdatedAt),
        new(2, "Назначение", CreatedAt, UpdatedAt)
    ];

    internal static readonly List<TagDto> Tags =
    [
        new(1, "Важное", 2, "Назначение", CreatedAt, UpdatedAt),
        new(2, "Работа", 1, "Проекты", CreatedAt, UpdatedAt)
    ];

    internal static readonly ContentStatusDto[] ContentStatuses =
    [
        new(1, "Черновик", CreatedAt, UpdatedAt),
        new(2, "Опубликовано", CreatedAt, UpdatedAt)
    ];

    internal static readonly List<ContentFileDto> ContentFiles =
    [
        new(1, "/api/content-files/1/content", 1, "Example.rfa", ".rfa", 1024, "primary", CreatedAt, UpdatedAt),
        new(2, "/api/content-files/2/content", null, "Preview.png", ".png", 512, "preview", CreatedAt, UpdatedAt)
    ];

    internal static readonly List<ContentVersionDto> ContentVersions =
    [
        new(1, 1, [1], "Версия 1.0", CreatedAt, "published", "revit", "2026", CreatedAt, UpdatedAt)
    ];

    internal static readonly List<ContentDto> Contents =
    [
        new(1, "Пример материала", "Тестовый материал из мок-сервиса.", 1, 2, 2, [1, 2], CreatedAt, UpdatedAt),
        new(2, "Рабочий документ", "Материал без превью.", 2, 1, null, [2], CreatedAt, UpdatedAt)
    ];

    internal static readonly List<ContentHistoryEventDto> ContentHistory =
    [
        new(1, 1, UpdatedAt, "Изменены данные контента.", "revex")
    ];

    internal static Task<IReadOnlyCollection<T>> FromCollection<T>(
        IEnumerable<T> items,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<T>>(items.ToArray());
    }

    internal static Task<T?> FromItem<T>(T? item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(item);
    }
}

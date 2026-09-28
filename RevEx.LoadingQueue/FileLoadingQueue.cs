using System.Collections.Concurrent;
using RevEx.Connector.Contracts;

namespace RevEx.LoadingQueue;

public sealed class FileLoadingQueue : IFileLoadingQueue
{
    private readonly ConcurrentDictionary<Guid, FileLoadingQueueItem> _items = new();

    public event EventHandler? Changed;

    public IReadOnlyCollection<FileLoadingQueueItem> GetAll() =>
        _items.Values.OrderByDescending(item => item.UpdatedAt).ToArray();

    public FileLoadingQueueItem Enqueue(Guid id, string fileName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификатор загрузки не может быть пустым.", nameof(id));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Имя файла не может быть пустым.", nameof(fileName));

        var item = new FileLoadingQueueItem(
            id,
            fileName.Trim(),
            FileLoadingStatus.Accepted,
            DateTimeOffset.UtcNow);
        if (!_items.TryAdd(id, item))
            throw new InvalidOperationException($"Загрузка {id} уже находится в очереди.");

        Changed?.Invoke(this, EventArgs.Empty);
        return item;
    }

    public bool UpdateStatus(Guid id, FileLoadingStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status), status, "Неизвестный статус загрузки.");

        var changed = false;
        _items.AddOrUpdate(
            id,
            _ => throw new KeyNotFoundException($"Загрузка {id} отсутствует в очереди."),
            (_, current) =>
            {
                if (status <= current.Status)
                    return current;

                changed = true;
                return current with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            });

        if (changed)
            Changed?.Invoke(this, EventArgs.Empty);
        return changed;
    }
}

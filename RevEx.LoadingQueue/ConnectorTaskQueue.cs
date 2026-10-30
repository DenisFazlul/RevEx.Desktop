using System.Collections.Concurrent;
using RevEx.Domain.Contracts.Connectors;

namespace RevEx.LoadingQueue;

public sealed class ConnectorTaskQueue : IConnectorTaskQueue
{
    private readonly ConcurrentDictionary<Guid, ConnectorTaskQueueItem> items = new();
    public event EventHandler? Changed;
    public IReadOnlyCollection<ConnectorTaskQueueItem> GetAll() => items.Values.OrderByDescending(item => item.UpdatedAt).ToArray();

    public ConnectorTaskQueueItem Enqueue(ConnectorTask task)
    {
        if (task.Id == Guid.Empty || string.IsNullOrWhiteSpace(task.Name) || !Enum.IsDefined(task.Command))
            throw new ArgumentException("Некорректная задача.", nameof(task));
        var candidate = new ConnectorTaskQueueItem(task, ConnectorTaskStatus.Sent, null, 0, DateTimeOffset.UtcNow);
        var current = items.GetOrAdd(task.Id, candidate);
        if (current.Task != task) throw new InvalidOperationException("Идентификатор уже используется другой задачей.");
        if (ReferenceEquals(current, candidate)) Changed?.Invoke(this, EventArgs.Empty);
        return current;
    }

    public bool Update(ConnectorTaskUpdate update)
    {
        if (!Enum.IsDefined(update.Status)) throw new ArgumentOutOfRangeException(nameof(update));
        while (true)
        {
            if (!items.TryGetValue(update.Id, out var current)) throw new KeyNotFoundException("Задача отсутствует: " + update.Id);
            if (current.Status == ConnectorTaskStatus.Done || update.Status < current.Status ||
                (update.Sequence > 0 && update.Sequence <= current.Sequence)) return false;
            var next = current with { Status = update.Status, Message = update.Message,
                Sequence = update.Sequence > 0 ? update.Sequence : current.Sequence + 1, UpdatedAt = DateTimeOffset.UtcNow };
            if (!items.TryUpdate(update.Id, next, current)) continue;
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}

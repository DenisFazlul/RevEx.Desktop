using RevEx.Domain.Contracts.Connectors;

namespace RevEx.LoadingQueue;

public sealed record ConnectorTaskQueueItem(
    ConnectorTask Task,
    ConnectorTaskStatus Status,
    string? Message,
    long Sequence,
    DateTimeOffset UpdatedAt)
{
    public Guid Id => Task.Id;
    public string Name => Task.Name;
    public ConnectorCommand Command => Task.Command;
}

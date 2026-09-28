using RevEx.Connector.Contracts;

namespace RevEx.LoadingQueue;

public sealed record FileLoadingQueueItem(
    Guid Id,
    string FileName,
    FileLoadingStatus Status,
    DateTimeOffset UpdatedAt);

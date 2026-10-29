using RevEx.Domain.Contracts.Connectors;

namespace RevEx.LoadingQueue;

public interface IFileLoadingQueue
{
    event EventHandler? Changed;
    IReadOnlyCollection<FileLoadingQueueItem> GetAll();
    FileLoadingQueueItem Enqueue(Guid id, string fileName);
    bool UpdateStatus(Guid id, FileLoadingStatus status);
}

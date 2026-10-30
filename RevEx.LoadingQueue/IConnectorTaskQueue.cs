using RevEx.Domain.Contracts.Connectors;

namespace RevEx.LoadingQueue;

public interface IConnectorTaskQueue
{
    event EventHandler? Changed;
    IReadOnlyCollection<ConnectorTaskQueueItem> GetAll();
    ConnectorTaskQueueItem Enqueue(ConnectorTask task);
    bool Update(ConnectorTaskUpdate update);
}

using System.Collections.Concurrent;
using RevEx.Connector.Contracts;

namespace RevEx.Desktop.Connectors;

public sealed class ConnectorRegistry : IConnectorRegistry
{
    private readonly ConcurrentDictionary<Guid, ConnectorDescriptor> _connectors = new();

    public event EventHandler? Changed;

    public IReadOnlyCollection<ConnectorDescriptor> GetAll() =>
        _connectors.Values.OrderBy(item => item.Name).ToArray();

    public void Register(ConnectorDescriptor connector)
    {
        _connectors[connector.InstanceId] = connector;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Remove(Guid instanceId)
    {
        var removed = _connectors.TryRemove(instanceId, out _);
        if (removed)
            Changed?.Invoke(this, EventArgs.Empty);
        return removed;
    }
}

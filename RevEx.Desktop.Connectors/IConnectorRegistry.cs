using RevEx.Connector.Contracts;

namespace RevEx.Desktop.Connectors;

public interface IConnectorRegistry
{
    event EventHandler? Changed;
    IReadOnlyCollection<ConnectorDescriptor> GetAll();
    void Register(ConnectorDescriptor connector);
    bool Remove(Guid instanceId);
}

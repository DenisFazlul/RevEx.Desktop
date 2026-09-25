namespace RevEx.Connector.Contracts;

public interface IConnectorClient
{
    Task<ConnectorHealthResponse> GetHealthAsync(
        ConnectorDescriptor connector,
        CancellationToken cancellationToken = default);

    Task<InspectFamilyResponse> InspectAsync(
        ConnectorDescriptor connector,
        string path,
        CancellationToken cancellationToken = default);

    Task<LoadFamilyResponse> LoadAsync(
        ConnectorDescriptor connector,
        string path,
        CancellationToken cancellationToken = default);

    Task<LoadContentVersionResponse> LoadContentVersionAsync(
        ConnectorDescriptor connector,
        IReadOnlyCollection<string> paths,
        CancellationToken cancellationToken = default);
}

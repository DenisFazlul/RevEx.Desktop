namespace RevEx.Connector.Contracts;

public interface IConnectorClient
{
    Task<ConnectorHealthResponse> GetHealthAsync(
        ConnectorDescriptor connector,
        CancellationToken cancellationToken = default);

    Task<LoadContentVersionResponse> LoadContentVersionAsync(
        ConnectorDescriptor connector,
        IReadOnlyCollection<ContentVersionLoadFile> files,
        Uri statusCallbackAddress,
        CancellationToken cancellationToken = default);
}

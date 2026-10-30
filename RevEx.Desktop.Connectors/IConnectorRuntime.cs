namespace RevEx.Desktop.Connectors;

public interface IConnectorRuntime : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

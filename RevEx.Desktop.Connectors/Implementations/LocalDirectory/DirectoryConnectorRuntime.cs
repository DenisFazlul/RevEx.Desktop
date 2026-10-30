using RevEx.RevitPlugin.Connector;
using RevEx.Connector.Contracts;

namespace RevEx.Desktop.Connectors.Implementations.LocalDirectory;

public sealed class DirectoryConnectorRuntime(
    IConnectorDiscovery discovery,
    IConnectorClient client,
    IConnectorRegistry registry) : IConnectorRuntime
{
    private readonly CancellationTokenSource stopping = new();
    private Task? running;
    private readonly SemaphoreSlim refresh = new(0, 1);
    private void DiscoveryChanged()
    {
        try { refresh.Release(); }
        catch (SemaphoreFullException) { }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (running is not null) return;
        discovery.Changed += DiscoveryChanged;
        await RefreshAsync(cancellationToken);
        running = Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        try
        {
            while (!stopping.IsCancellationRequested)
            {
                await refresh.WaitAsync(TimeSpan.FromSeconds(5), stopping.Token);
                await RefreshAsync(stopping.Token);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }

    private async Task RefreshAsync(CancellationToken token)
    {
        var descriptors = await discovery.FindAsync(token);
        var ids = descriptors.Select(item => item.InstanceId).ToHashSet();
        foreach (var old in registry.GetAll())
            if (!ids.Contains(old.InstanceId)) registry.Remove(old.InstanceId);
        await Task.WhenAll(descriptors.Select(async descriptor =>
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            try
            {
                await client.GetHealthAsync(descriptor, timeout.Token);
                if (!registry.GetAll().Contains(descriptor)) registry.Register(descriptor);
            }
            catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or
                OperationCanceledException or ObjectDisposedException or InvalidOperationException or Newtonsoft.Json.JsonException)
            {
                registry.Remove(descriptor.InstanceId);
                // Keep its file: a timeout alone does not prove the owning process is dead.
            }
        }));
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        stopping.Cancel();
        discovery.Changed -= DiscoveryChanged;
        if (running is not null) await running;
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}

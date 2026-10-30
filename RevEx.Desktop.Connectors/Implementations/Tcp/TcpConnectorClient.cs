using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using RevEx.RevitPlugin.Connector;
using RevEx.Connector.Contracts;
using RevEx.Domain.Contracts.Connectors;
using RevEx.LoadingQueue;

namespace RevEx.Desktop.Connectors.Implementations.Tcp;

public sealed class TcpConnectorClient(
    IConnectorTransport transport,
    IConnectorTaskQueue loadingQueue) : IConnectorClient, IDisposable
{
    private readonly ConcurrentDictionary<Guid, IConnectorConnection> connections = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> connectionLocks = new();
    private bool disposed;

    public async Task<ConnectorHealthResponse> GetHealthAsync(ConnectorDescriptor connector,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        var connection = await GetConnectionAsync(connector, timeout.Token);
        var result = (await connection.RequestAsync(ConnectorCommand.Health, JValue.CreateNull(), timeout.Token))
            .ToObject<ConnectorHealthResponse>() ?? throw new IOException("Missing health response.");
        if (result.InstanceId != connector.InstanceId)
        {
            connection.Dispose();
            throw new IOException("Connector instance does not match its registration.");
        }
        return result;
    }

    public async Task<TResponse> ExecuteAsync<TRequest, TResponse>(ConnectorDescriptor connector,
        ConnectorTask task, TRequest request, CancellationToken cancellationToken = default)
    {
        loadingQueue.Enqueue(task);
        var connection = await GetConnectionAsync(connector, cancellationToken);
        var response = await connection.RequestAsync(task,
            request is null ? JValue.CreateNull() : JToken.FromObject(request), cancellationToken);
        return response.ToObject<TResponse>() ?? throw new IOException("Missing task response.");
    }

    public async Task<LoadContentVersionResponse> LoadContentVersionAsync(ConnectorDescriptor connector,
        IReadOnlyCollection<ContentVersionLoadFile> files, CancellationToken cancellationToken = default)
    {
        var loaded = 0;
        foreach (var file in files)
        {
            // The Desktop creates the task identity; the connector only echoes that identity.
            var task = new ConnectorTask(file.Id, file.Name, ConnectorCommand.LoadContentVersion);
            var result = await ExecuteAsync<ConnectorLoadRequest, LoadContentVersionResponse>(connector, task,
                new ConnectorLoadRequest { Files = new[] { file } }, cancellationToken);
            if (!result.Success) return new LoadContentVersionResponse(false, loaded);
            loaded += result.LoadedFileCount;
        }
        return new LoadContentVersionResponse(true, loaded);
    }

    private async Task<IConnectorConnection> GetConnectionAsync(ConnectorDescriptor connector, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        token = timeout.Token;
        var connecting = connectionLocks.GetOrAdd(connector.InstanceId, _ => new SemaphoreSlim(1, 1));
        await connecting.WaitAsync(token);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (connections.TryGetValue(connector.InstanceId, out var existing) && existing.IsConnected) return existing;
            var connection = await transport.ConnectAsync(connector.BaseAddress, token);
            try
            {
                connection.NotificationReceived += (name, payload) =>
                {
                    if (name == "taskStatus" && payload.ToObject<ConnectorTaskUpdate>() is { } update)
                        ApplyStatus(update);
                };
                connection.Start();
                var health = (await connection.RequestAsync(ConnectorCommand.Health, JValue.CreateNull(), token)).ToObject<ConnectorHealthResponse>();
                if (health?.InstanceId != connector.InstanceId) throw new IOException("Stale connector registration.");
                var statuses = (await connection.RequestAsync(ConnectorCommand.GetTaskStatuses, JValue.CreateNull(), token))
                    .ToObject<ConnectorTaskUpdate[]>() ?? [];
                foreach (var status in statuses) ApplyStatus(status);
                connections[connector.InstanceId] = connection;
                return connection;
            }
            catch { connection.Dispose(); throw; }
        }
        finally { connecting.Release(); }
    }

    private void ApplyStatus(ConnectorTaskUpdate update)
    {
        // A restarted Desktop may have no corresponding queue item. Other items still update.
        try { loadingQueue.Update(update); }
        catch (KeyNotFoundException) { }
        catch (ArgumentOutOfRangeException) { }
    }

    public void Dispose()
    {
        disposed = true;
        foreach (var connection in connections.Values) connection.Dispose();
        transport.Dispose();
    }
}

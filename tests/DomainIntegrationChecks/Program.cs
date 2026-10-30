using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using RevEx.RevitPlugin.Connector;
using RevEx.RevitPlugin.Connector.Commands;
using RevEx.RevitPlugin.Connector.Implementations.LocalDirectory;
using RevEx.RevitPlugin.Connector.Implementations.Tcp;
using RevEx.Connector.Host;
using RevEx.Desktop.Connectors;
using RevEx.Desktop.Connectors.Implementations.LocalDirectory;
using RevEx.Desktop.Connectors.Implementations.Tcp;
using RevEx.Domain.Contracts.Connectors;
using RevEx.LoadingQueue;

static void Require(bool value, string message) { if (!value) throw new Exception(message); }
static async Task UntilAsync(Func<bool> condition)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    while (!condition()) await Task.Delay(30, timeout.Token);
}
var directory = Path.Combine(Path.GetTempPath(), "RevEx-tasks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
Process? framework = null;
try
{
    using var discovery = new FileConnectorDiscovery(directory);
    await using var modern = new ConnectorHost(new ConnectorHostOptions("Modern", ConnectorProduct.Mock, "1", DiscoveryDirectory: directory));
    var calls = 0;
    var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    ConnectorTask? received = null;
    modern.RegisterExecutor(new TestExecutor(async (request, context, token) =>
    {
        Interlocked.Increment(ref calls);
        received = context.Task;
        if (request.Files.Count == 0) throw new ArgumentException("No files");
        await context.ReportStatusAsync("Opening document", token);
        started.TrySetResult();
        await proceed.Task.WaitAsync(token);
        await context.ReportStatusAsync("Loading family", token);
        return new LoadContentVersionResponse(true, request.Files.Count);
    }));
    await modern.StartAsync();
    var descriptor = (await discovery.FindAsync(default)).Single();
    await File.WriteAllTextAsync(Path.Combine(directory, "broken.json"), "{");
    await discovery.PublishAsync(descriptor with { InstanceId = Guid.NewGuid() }, default);
    await discovery.PublishAsync(descriptor with { InstanceId = Guid.NewGuid(), BaseAddress = new Uri("tcp://192.0.2.1:1234") }, default);
    var queue = new ConnectorTaskQueue();
    var registry = new ConnectorRegistry();
    using var client = new TcpConnectorClient(new TcpConnectorTransport(), queue);
    await using var runtime = new DirectoryConnectorRuntime(discovery, client, registry);
    await runtime.StartAsync();
    Require(registry.GetAll().Count == 1, "Stale/invalid registrations accepted");
    Console.WriteLine("PASS: connector starts without Desktop; discovery validates registrations.");

    // Task identity is independent of file identity and payload.
    var task = new ConnectorTask(Guid.NewGuid(), "Load selected version", ConnectorCommand.LoadContentVersion);
    var file = new ContentVersionLoadFile(Guid.NewGuid(), "Family.rfa", "Family.rfa");
    var request = new ConnectorLoadRequest { Files = new[] { file } };
    queue.Enqueue(task);
    Require(queue.GetAll().Single(item => item.Id == task.Id).Status == ConnectorTaskStatus.Sent, "Initial status must be Sent");
    var load = client.ExecuteAsync<ConnectorLoadRequest, LoadContentVersionResponse>(descriptor, task, request);
    await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
    await UntilAsync(() => queue.GetAll().Single(item => item.Id == task.Id).Message == "Opening document");
    Require(received == task && received.Id != file.Id, "Task metadata/identity was replaced with a file ID");
    Require(queue.GetAll().Single(item => item.Id == task.Id).Status == ConnectorTaskStatus.Accepted, "Informational text changed lifecycle status");
    await client.GetHealthAsync(descriptor);
    var busy = new ConnectorTask(Guid.NewGuid(), "Second task", ConnectorCommand.LoadContentVersion);
    try
    {
        await client.ExecuteAsync<ConnectorLoadRequest, LoadContentVersionResponse>(descriptor, busy, request);
        throw new Exception("Concurrent exclusive task accepted");
    }
    catch (InvalidOperationException) { }
    Require(queue.GetAll().Single(item => item.Id == busy.Id).Status == ConnectorTaskStatus.Done,
        "Rejected task did not finish");
    await runtime.StopAsync();
    client.Dispose();
    try { await load; throw new Exception("Pending request survived disconnect"); } catch (IOException) { }
    proceed.SetResult();
    using var reconnected = new TcpConnectorClient(new TcpConnectorTransport(), queue);
    await reconnected.GetHealthAsync(descriptor);
    await UntilAsync(() => queue.GetAll().Single(item => item.Id == task.Id).Status == ConnectorTaskStatus.Done);
    Require(queue.GetAll().Single(item => item.Id == task.Id).Message == "Loading family", "Done discarded the informational message");
    var result = await reconnected.ExecuteAsync<ConnectorLoadRequest, LoadContentVersionResponse>(descriptor, task, request);
    Require(result.Success && calls == 1, "Retry executed the same task twice");
    // Old informational notifications cannot replace a completed state.
    Require(!queue.Update(new(task.Id, ConnectorTaskStatus.Accepted, "stale", 1)), "Stale notification accepted");
    Console.WriteLine("PASS: Sent/Accepted/Done, independent task ID, informational text, disconnect/reconnect and idempotent retry.");

    var failed = new ConnectorTask(Guid.NewGuid(), "Invalid request", ConnectorCommand.LoadContentVersion);
    try
    {
        await reconnected.ExecuteAsync<ConnectorLoadRequest, LoadContentVersionResponse>(descriptor, failed, new ConnectorLoadRequest());
        throw new Exception("Invalid request succeeded");
    }
    catch (InvalidOperationException) { }
    var failedState = queue.GetAll().Single(item => item.Id == failed.Id);
    Require(failedState.Status == ConnectorTaskStatus.Done && failedState.Message?.Contains("No files") == true,
        "Error must complete the task with informational text, without an extra lifecycle status");
    await reconnected.GetHealthAsync(descriptor);
    Console.WriteLine("PASS: errors return separately and use Done plus informational text; the connection remains usable.");

    var healthTask = new ConnectorTask(Guid.NewGuid(), "Check application", ConnectorCommand.Health);
    await reconnected.ExecuteAsync<object?, ConnectorHealthResponse>(descriptor, healthTask, null);
    Require(queue.GetAll().Single(item => item.Id == healthTask.Id).Status == ConnectorTaskStatus.Done,
        "Generic task lifecycle depended on command type");

    if (args.Length > 0)
    {
        var start = new ProcessStartInfo(Path.GetFullPath(args[0]))
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(directory);
        framework = Process.Start(start)!;
        Require(await framework.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)) == "READY", "Framework host failed");
        var registry2 = new ConnectorRegistry();
        await using var runtime2 = new DirectoryConnectorRuntime(discovery, reconnected, registry2);
        await runtime2.StartAsync();
        var legacy = registry2.GetAll().Single(item => item.Product == ConnectorProduct.Revit);
        var legacyFile = new ContentVersionLoadFile(Guid.NewGuid(), "Legacy.rfa", "Legacy.rfa");
        var legacyResult = await reconnected.LoadContentVersionAsync(legacy, new[] { legacyFile });
        Require(legacyResult.Success && queue.GetAll().Single(item => item.Id == legacyFile.Id).Status == ConnectorTaskStatus.Done,
            "Framework task/status contract incompatible");
        await framework.StandardInput.WriteLineAsync();
        await framework.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await UntilAsync(() => registry2.GetAll().All(item => item.InstanceId != legacy.InstanceId));
        Console.WriteLine("PASS: .NET Framework 4.7.2 / .NET 8 task execution and discovery cleanup.");
    }
    await modern.StopAsync();
    Require(!File.Exists(Path.Combine(directory, modern.InstanceId + ".json")), "Registration not cleaned up");
}
finally
{
    if (framework is { HasExited: false }) framework.Kill();
    framework?.Dispose();
    Directory.Delete(directory, true);
}

internal sealed class TestExecutor(
    Func<ConnectorLoadRequest, IConnectorCommandContext, CancellationToken, Task<LoadContentVersionResponse>> execute)
    : ConnectorCommandExecutor<ConnectorLoadRequest, LoadContentVersionResponse>
{
    public override ConnectorCommand Command => ConnectorCommand.LoadContentVersion;
    protected override Task<LoadContentVersionResponse> ExecuteAsync(ConnectorLoadRequest request,
        IConnectorCommandContext context, CancellationToken token) => execute(request, context, token);
}

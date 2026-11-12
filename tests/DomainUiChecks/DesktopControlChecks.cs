using System.Net.Http.Json;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using RevEx.Connector.Client;
using RevEx.Connector.Contracts;
using RevEx.Desktop.Services;
using RevEx.Domain.Contracts.Connectors;

internal static class DesktopControlChecks
{
    public static void Run()
    {
        Task.Run(RunAsync).GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "RevEx.DesktopControlChecks",
            Guid.NewGuid().ToString("N"));
        var registrationFile = Path.Combine(temporaryDirectory, "desktop-control.json");
        var connector = new ConnectorDescriptor(
            Guid.NewGuid(),
            "Test Revit",
            ConnectorProduct.Revit,
            "2026",
            new Uri("http://127.0.0.1:1"),
            Environment.ProcessId,
            "Model.rvt")
        {
            Commands = new[]
            {
                CreateLoadContract()
            }
        };
        var registry = new ConnectorRegistry();
        registry.Register(connector);
        var client = new TestConnectorClient();
        var bridge = new DesktopControlBridge(
            registry,
            client,
            new DesktopControlOptions(registrationFile));

        try
        {
            await bridge.StartAsync();

            Require(File.Exists(registrationFile), "Desktop registration file was not created.");
            var registrationJson = await File.ReadAllTextAsync(registrationFile);
            var registration = JsonSerializer.Deserialize<DesktopControlRegistration>(
                registrationJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            Require(registration is not null, "Desktop registration file is invalid.");

            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(registration!.Address)
            };
            httpClient.DefaultRequestHeaders.Add(
                DesktopControlBridge.AccessTokenHeader,
                registration.AccessToken);

            var health = await httpClient.GetFromJsonAsync<DesktopControlHealth>("health");
            Require(
                health?.InstanceId == registration.InstanceId,
                "Desktop health does not match registration.");

            var connectors = await httpClient.GetFromJsonAsync<DesktopConnectorDescription[]>(
                "connectors");
            var connectorDescriptions = connectors
                ?? throw new InvalidOperationException("Desktop connector list is empty.");
            Require(
                connectorDescriptions.Single().InstanceId == connector.InstanceId,
                "Desktop connector list is invalid.");
            Require(
                connectorDescriptions.Single().Commands.Single().Name == nameof(ConnectorCommand.LoadContentVersion),
                "Desktop connector list does not contain command contracts.");

            var commands = await httpClient.GetStringAsync(
                $"connectors/{connector.InstanceId:D}/commands");
            Require(
                commands.Contains("LoadContentVersion", StringComparison.Ordinal),
                "Desktop connector commands are missing.");

            var executeResponse = await httpClient.PostAsJsonAsync(
                $"connectors/{connector.InstanceId:D}/commands/LoadContentVersion",
                new
                {
                    taskName = "Load from MCP",
                    payload = new
                    {
                        files = Array.Empty<object>()
                    }
                });
            executeResponse.EnsureSuccessStatusCode();
            var executeJson = await executeResponse.Content.ReadAsStringAsync();

            Require(
                executeJson.Contains("LoadContentVersion", StringComparison.Ordinal),
                "Desktop command response is invalid.");
            Require(
                client.LastCommand == ConnectorCommand.LoadContentVersion,
                "Desktop did not pass the command to IConnectorClient.");
        }
        finally
        {
            await bridge.StopAsync();
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        Require(
            !File.Exists(registrationFile),
            "Desktop registration file was not removed on shutdown.");
        Console.WriteLine("PASS: Desktop MCP control bridge discovery and command execution.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static ConnectorCommandContract CreateLoadContract()
    {
        var input = new ConnectorPayloadContract(
            "RevEx.Connector.Protocol.ConnectorLoadRequest",
            Array.Empty<ConnectorObjectContract>(),
            Array.Empty<ConnectorEnumContract>());
        var output = new ConnectorPayloadContract(
            "RevEx.Domain.Contracts.Connectors.LoadContentVersionResponse",
            Array.Empty<ConnectorObjectContract>(),
            Array.Empty<ConnectorEnumContract>());

        return new ConnectorCommandContract(
            ConnectorCommand.LoadContentVersion,
            "load",
            true,
            input,
            output);
    }

    private sealed class TestConnectorClient : IConnectorClient
    {
        public ConnectorCommand? LastCommand { get; private set; }

        public Task<TResponse> ExecuteAsync<TRequest, TResponse>(
            ConnectorDescriptor connector,
            ConnectorTask task,
            TRequest request,
            CancellationToken cancellationToken = default)
        {
            LastCommand = task.Command;
            object response;
            if (typeof(TResponse) == typeof(JToken))
            {
                response = new JObject
                {
                    ["success"] = true
                };
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported test response type {typeof(TResponse).Name}.");
            }

            return Task.FromResult((TResponse)response);
        }

        public Task<ConnectorHealthResponse> GetHealthAsync(
            ConnectorDescriptor connector,
            CancellationToken cancellationToken = default)
        {
            var response = new ConnectorHealthResponse(
                "healthy",
                connector.InstanceId,
                connector.ActiveDocument);

            return Task.FromResult(response);
        }

        public Task<LoadContentVersionResponse> LoadContentVersionAsync(
            ConnectorDescriptor connector,
            IReadOnlyCollection<ContentVersionExecuteDto> files,
            CancellationToken cancellationToken = default)
        {
            var response = new LoadContentVersionResponse(true, files.Count);

            return Task.FromResult(response);
        }
    }
}

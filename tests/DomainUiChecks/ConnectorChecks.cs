using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RevEx.Connector.Client;
using RevEx.Connector.Commands;
using RevEx.Connector.Contracts;
using RevEx.Connector.Host;
using RevEx.Connector.Protocol;
using RevEx.Connector.Protocol.Implementations.LocalDirectory;
using RevEx.Desktop.DI;
using RevEx.Desktop.ViewModels.Connectors;
using RevEx.Desktop.ViewModels.Loading;
using RevEx.Domain.Contracts.Connectors;
using RevEx.LoadingQueue;

internal static class ConnectorChecks
{
    public static void Run()
    {
        var queue = Task.Run(CheckDesktopServicesAsync).GetAwaiter().GetResult();
        var viewModel = new LoadingQueueTabViewModel(queue);
        Dispatcher.UIThread.RunJobs();
        Require(viewModel.HasItems && viewModel.Items.All(item => item.StatusText == "Готово"),
            "Desktop loading tab did not display completed tasks.");
        Require(viewModel.Items.Any(item => item.Message == "Файл загружен"),
            "Desktop loading tab lost the connector status message.");
        Console.WriteLine("PASS: Desktop DI, late connector discovery, health UI, command discovery, file dispatch and loading tab.");
    }

    private static async Task<IConnectorTaskQueue> CheckDesktopServicesAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "RevEx-Desktop-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
                .Build();
            var services = new ServiceCollection();
            services.AddServices(configuration);
            services.AddSingleton<IConnectorDiscovery>(new FileConnectorDiscovery(directory));
            await using var provider = services.BuildServiceProvider();
            var runtime = provider.GetRequiredService<IConnectorRuntime>();
            var registry = provider.GetRequiredService<IConnectorRegistry>();
            var client = provider.GetRequiredService<IConnectorClient>();
            var queue = provider.GetRequiredService<IConnectorTaskQueue>();
            await runtime.StartAsync();
            Require(registry.GetAll().Count == 0, "Isolated registry is not empty.");

            await using var connector = new ConnectorHost(new ConnectorHostOptions(
                "Desktop integration connector", ConnectorProduct.Mock, "1", DiscoveryDirectory: directory));
            connector.RegisterExecutor(new LoadExecutor());
            await connector.StartAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (registry.GetAll().Count == 0)
            {
                await Task.Delay(50, timeout.Token);
            }

            var descriptor = registry.GetAll().Single();
            var item = new ConnectorItemViewModel(descriptor, client, id => registry.Remove(id));
            await item.PingAsync(timeout.Token);
            Require(item.IsAvailable && item.Status == "Доступен", "Desktop health check failed.");
            var loadContract = descriptor.Commands.SingleOrDefault(
                command => command.Command == ConnectorCommand.LoadContentVersion)
                ?? throw new InvalidOperationException(
                    "Registered command is not available to Desktop.");
            Require(
                loadContract.Input.Objects.Any(contract =>
                    contract.Name == nameof(ConnectorLoadRequest) &&
                    contract.Properties.Any(property => property.Name == "files")),
                "Connector command input contract is missing.");
            Require(
                loadContract.Output.Objects.Any(contract =>
                    contract.Name == nameof(LoadContentVersionResponse)),
                "Connector command output contract is missing.");
            var file = Path.Combine(directory, "Family.rfa");
            await File.WriteAllTextAsync(file, "Transport test fixture", timeout.Token);
            var id = Guid.NewGuid();
            var response = await client.LoadContentVersionAsync(descriptor,
                new[] { new ContentVersionExecuteDto(id, "Family.rfa", file) }, timeout.Token);
            Require(response.Success && response.LoadedFileCount == 1, "Desktop file dispatch failed.");
            Require(queue.GetAll().Single(task => task.Id == id).Status == ConnectorTaskStatus.Done,
                "Desktop queue did not receive completion.");
            await connector.StopAsync(timeout.Token);
            while (registry.GetAll().Count != 0)
            {
                await Task.Delay(50, timeout.Token);
            }

            await runtime.StopAsync(timeout.Token);
            return queue;
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class LoadExecutor : ConnectorCommandExecutor<ConnectorLoadRequest, LoadContentVersionResponse>
    {
        public override ConnectorCommand Command => ConnectorCommand.LoadContentVersion;

        protected override async Task<LoadContentVersionResponse> ExecuteAsync(
            ConnectorLoadRequest request, IConnectorCommandContext context, CancellationToken cancellationToken)
        {
            Require(request.Files.Count == 1 && File.Exists(request.Files.Single().Path),
                "Connector did not receive the existing local file.");
            await context.ReportStatusAsync("Файл загружен", cancellationToken);
            return new LoadContentVersionResponse(true, request.Files.Count);
        }
    }
}

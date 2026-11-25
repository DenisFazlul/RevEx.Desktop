using System.Text.Json;
using RevEx.Connector.Client;
using RevEx.Connector.Contracts;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.Services;
using RevEx.Desktop.Services.VersionImport;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Entities;
using RevEx.Domain.Identifiers;

internal static class UnifiedVersionImportChecks
{
    public static void Run()
    {
        CheckAsync().GetAwaiter().GetResult();
        Console.WriteLine("PASS: unified connector read/prepare/upload flow and in-process desktop connector.");
    }

    private static async Task CheckAsync()
    {
        var api = new MockRevExApiService();
        var client = new UnusedConnectorClient();
        var registry = new ConnectorRegistry();
        var rfaImport = new RfaFamilyImportService(api, client, registry);
        var import = new VersionImportService(api, rfaImport);
        var category = (await api.Categories.GetAllAsync()).First();
        var content = await api.Contents.CreateAsync(new CreateContentDto
        {
            Cid = Uuid7.Create(),
            Name = "Unified import",
            Description = "Unified import",
            CategoryId = category.Id
        });
        var path = Path.Combine(Path.GetTempPath(), $"RevEx-unified-{Guid.NewGuid():N}.dwg");
        File.WriteAllBytes(path, [1, 2, 3, 4]);

        try
        {
            var desktop = new DesktopFileConnector();
            var catalog = new VersionFileConnectorCatalog(registry, client, desktop);
            Require(catalog.GetAll(Platform.AutoCad).Single().Id == DesktopFileConnector.ConnectorId,
                "The in-process connector was not exposed for AutoCAD.");
            Require(catalog.GetAll(Platform.Revit).Count == 0,
                "The plain-file connector was exposed as a Revit native-file editor.");

            var connector = new RecordingConnector();
            using var session = await import.ReadAsync(content, Platform.AutoCad, connector, [path]);
            var version = await import.SaveAsync(session, "DWG version", "2026");

            Require(connector.ReadCount == 1 && connector.PrepareCount == 1,
                "The common entry point did not invoke both connector phases.");
            Require(connector.PreparedCid == content.Cid && Uuid7.IsValid(connector.PreparedVid),
                "The connector did not receive the content CID and a generated VID.");
            Require(version.Platform == Platform.AutoCad && version.ContentId == content.Id && version.Vid == connector.PreparedVid,
                "The version did not preserve the platform identifiers returned through the common flow.");

            var file = (await api.ContentFiles.GetAllAsync()).Single(item => item.ContentVersionId == version.Id);
            Require(file.Role == "primary" && file.Metadata is JsonElement,
                "The prepared file or its connector metadata was not uploaded.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class RecordingConnector : IVersionFileConnector
    {
        public string Id => "recording-autocad";

        public string Name => "Recording AutoCAD";

        public ConnectorDescriptor? RemoteDescriptor => null;

        public int ReadCount { get; private set; }

        public int PrepareCount { get; private set; }

        public Guid PreparedCid { get; private set; }

        public Guid PreparedVid { get; private set; }

        public bool SupportsPlatform(Platform platform)
        {
            return platform == Platform.AutoCad;
        }

        public Task<IReadOnlyCollection<ConnectorFileMetadata>> ReadAsync(
            IReadOnlyCollection<ContentVersionExecuteDto> files,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            var result = files.Select(file => new ConnectorFileMetadata(
                file,
                "{\"Source\":\"AutoCAD connector\"}")).ToArray();
            return Task.FromResult<IReadOnlyCollection<ConnectorFileMetadata>>(result);
        }

        public Task<IReadOnlyCollection<ContentVersionExecuteDto>> PrepareAsync(
            Guid cid,
            Guid vid,
            Platform platform,
            IReadOnlyCollection<ContentVersionExecuteDto> files,
            CancellationToken cancellationToken = default)
        {
            PrepareCount++;
            PreparedCid = cid;
            PreparedVid = vid;
            return Task.FromResult(files);
        }
    }

    private sealed class UnusedConnectorClient : IConnectorClient
    {
        public Task<TResponse> ExecuteAsync<TRequest, TResponse>(
            ConnectorDescriptor connector,
            ConnectorTask task,
            TRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ConnectorHealthResponse> GetHealthAsync(
            ConnectorDescriptor connector,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<LoadContentVersionResponse> LoadContentVersionAsync(
            ConnectorDescriptor connector,
            IReadOnlyCollection<ContentVersionExecuteDto> files,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}

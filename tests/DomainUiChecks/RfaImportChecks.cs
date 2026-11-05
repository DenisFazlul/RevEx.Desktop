using Newtonsoft.Json.Linq;
using RevEx.Connector;
using RevEx.Connector.Client;
using RevEx.Connector.Client.Implementations.Tcp;
using RevEx.Connector.Commands;
using RevEx.Connector.Host;
using RevEx.Connector.Protocol.Implementations.Tcp;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.Services;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.LoadingQueue;

internal static class RfaImportChecks
{
    public static void Run()
    {
        Task.Run(CheckAsync).GetAwaiter().GetResult();
        Console.WriteLine("PASS: RFA reader selection, form autofill, exact name matching, new versions, unknown metadata, upload retry and source preservation.");
    }

    private static async Task CheckAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "RevEx-Rfa-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var mock = new MockRevExApiService();
            var versions = new RetryVersionRepository(mock.ContentVersions);
            var api = new RevExApiService(mock.Contents, mock.Categories, mock.Tags, mock.TagGroups,
                mock.ContentStatuses, versions, mock.ContentFiles, mock.ContentHistory);
            var registry = new ConnectorRegistry();
            using var transport = new TcpConnectorTransport();
            using var client = new TcpConnectorClient(transport, new ConnectorTaskQueue());
            var import = new RfaFamilyImportService(api, client, registry);
            var name = "Rfa-import-" + Guid.NewGuid().ToString("N");
            var path = Path.Combine(directory, name + ".rfa");
            await File.WriteAllTextAsync(path, "Original RFA fixture");
            var original = await File.ReadAllBytesAsync(path);
            using (var unavailable = new RfaFamilyImportViewModel(api, import))
            {
                await unavailable.LoadFileAsync(path);
                Require(unavailable.HasError && !unavailable.CanSave, "Missing reader must block saving.");
            }

            await using var unsupported = new ConnectorHost(new ConnectorHostOptions(
                "Revit without reader", ConnectorProduct.Revit, "2027", DiscoveryDirectory: directory));
            await unsupported.StartAsync();
            registry.Register(Describe(unsupported, ConnectorProduct.Revit, "2027"));
            await using var reader = new ConnectorHost(new ConnectorHostOptions(
                "RFA mock reader", ConnectorProduct.Mock, "2026", DiscoveryDirectory: directory));
            reader.RegisterExecutor(new ReadExecutor());
            await reader.StartAsync();
            registry.Register(Describe(reader, ConnectorProduct.Mock, "2026"));
            var category = (await api.Categories.GetAllAsync()).First();
            await api.Contents.CreateAsync(new CreateContentDto
            {
                Name = name + "-similar",
                Description = "Substring must not match",
                CategoryId = category.Id
            });

            var before = (await api.ContentVersions.GetAllAsync()).Count;
            int contentId;
            using (var first = new RfaFamilyImportViewModel(api, import))
            {
                await first.LoadFileAsync(path);
                Require(!first.HasError && first.CanSave && first.IsNewContent, first.Message ?? "Reading failed.");
                Require(first.ContentName == name && first.ConnectorName == "RFA mock reader", "Name or automatic reader selection failed.");
                Require(first.FamilyTypes.Count == 1 && first.Description == "Mock description" && first.ApplicationVersion == "2026", "Form was not populated.");
                Require(JObject.Parse(first.MetadataJson)["future"]?["nested"]?[1]?["value"]?.Value<int>() == 42, "Unknown connector fields were lost.");
                first.SelectedCategory = category;
                versions.FailNextUpload = true;
                Require(!await first.SaveAsync(), "Expected an upload failure.");
                Require((await api.ContentVersions.GetAllAsync()).Count == before + 1, "Version was not retained for retry.");
                Require(await first.SaveAsync(), first.Message ?? "Retry failed.");
                contentId = first.SavedVersion!.ContentId;
                Require((await api.ContentVersions.GetAllAsync()).Count == before + 1, "Retry created another version.");
                var file = await api.ContentFiles.GetByIdAsync(first.SavedVersion.FileIds.Single());
                var metadata = JObject.Parse(System.Text.Json.JsonSerializer.Serialize(file!.Metadata));
                Require(metadata["future"]?["nested"]?[1]?["value"]?.Value<int>() == 42, "Metadata was not saved with the file.");
                Require(!first.CanSave, "Completed import can be saved twice.");
            }

            using (var second = new RfaFamilyImportViewModel(api, import))
            {
                await second.LoadFileAsync(path);
                Require(!second.IsNewContent && !second.CanEditContent, "Existing content was not detected.");
                Require(await second.SaveAsync(), second.Message ?? "Second import failed.");
                Require(second.SavedVersion!.ContentId == contentId, "Second import created a different card.");
            }
            Require((await api.ContentVersions.GetAllAsync()).Count == before + 2, "Each completed import must create a new version.");
            Require((await api.Contents.GetAllAsync(new ContentQueryDto { Name = name })).Count == 2, "Duplicate content created.");
            Require(original.SequenceEqual(await File.ReadAllBytesAsync(path)), "Connector modified the user's original file.");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static ConnectorDescriptor Describe(ConnectorHost host, ConnectorProduct product, string version)
    {
        return new ConnectorDescriptor(host.InstanceId, product == ConnectorProduct.Mock ? "RFA mock reader" : "Revit without reader",
            product, version, host.BaseAddress!, Environment.ProcessId, null);
    }

    private static void Require(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class ReadExecutor : ConnectorCommandExecutor<ConnecterReadFileRequest, JObject>
    {
        public override ConnectorCommand Command => ConnectorCommand.ReadFiles;

        protected override async Task<JObject> ExecuteAsync(ConnecterReadFileRequest request, IConnectorCommandContext context, CancellationToken token)
        {
            var file = request.Files.Single();
            await File.AppendAllTextAsync(file.Path, "Prepared by connector", token);
            return new JObject
            {
                ["FamilyReadResults"] = new JArray(new JObject
                {
                    ["Name"] = file.Name,
                    ["Path"] = file.Path,
                    ["Description"] = "Mock description",
                    ["ApplicationVersion"] = "2026",
                    ["Types"] = new JArray(new JObject { ["Name"] = "Type A", ["Parameters"] = new JArray() }),
                    ["future"] = new JObject { ["nested"] = new JArray("unknown", new JObject { ["value"] = 42 }) }
                })
            };
        }
    }

    private sealed class RetryVersionRepository(IContentVersionRepository inner) : IContentVersionRepository
    {
        public bool FailNextUpload { get; set; }

        public Task<IReadOnlyCollection<ContentVersionDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return inner.GetAllAsync(cancellationToken);
        }

        public Task<ContentVersionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return inner.GetByIdAsync(id, cancellationToken);
        }

        public Task<ContentVersionDto> CreateAsync(CreateContentVersionDto request, CancellationToken cancellationToken = default)
        {
            return inner.CreateAsync(request, cancellationToken);
        }

        public Task<ContentVersionDto> UploadFileAsync(int versionId, Stream content, string fileName, string role,
            CancellationToken cancellationToken = default, string? metadataJson = null)
        {
            if (FailNextUpload)
            {
                FailNextUpload = false;
                throw new HttpRequestException("Simulated upload interruption.");
            }

            return inner.UploadFileAsync(versionId, content, fileName, role, cancellationToken, metadataJson);
        }

        public Task DeleteFileAsync(int versionId, int fileId, CancellationToken cancellationToken = default)
        {
            return inner.DeleteFileAsync(versionId, fileId, cancellationToken);
        }
    }
}

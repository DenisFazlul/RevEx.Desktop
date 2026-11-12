using Newtonsoft.Json.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RevEx.Connector;
using RevEx.Connector.Client;
using RevEx.Connector.Client.Implementations.Tcp;
using RevEx.Connector.Commands;
using RevEx.Connector.Host;
using RevEx.Connector.Protocol.Implementations.LocalDirectory;
using RevEx.Connector.Protocol.Implementations.Tcp;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.Services;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Contents;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;
using RevEx.LoadingQueue;

internal static class RfaImportChecks
{
    public static void Run()
    {
        CheckConnectorSelection();
        Task.Run(CheckAsync).GetAwaiter().GetResult();
        Console.WriteLine("PASS: RFA connector selection, deferred RFA-only reading, CSV/IFC attachments, Revit draft platform/status, metadata, upload retries without duplicate files and source preservation.");
    }

    private static void CheckConnectorSelection()
    {
        var api = new MockRevExApiService();
        var registry = new ConnectorRegistry();
        using var transport = new TcpConnectorTransport();
        using var client = new TcpConnectorClient(transport, new ConnectorTaskQueue());
        var import = new RfaFamilyImportService(api, client, registry);
        using var form = new RfaFamilyImportViewModel(api, import);
        var window = new RfaFamilyImportWindow(form);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var openButton = window.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content as string == "Открыть");
        var connectorBox = window.GetVisualDescendants().OfType<ComboBox>()
            .Single(box => ReferenceEquals(box.ItemsSource, form.Connectors));
        Require(!openButton.IsEnabled && !form.CanChooseFile && !form.IsBusy && !form.CanSave,
            "An empty form must wait for a connector and an explicit Open click.");
        var first = new ConnectorDescriptor(Guid.NewGuid(), "First", ConnectorProduct.Mock, "2026",
            new Uri("tcp://127.0.0.1:1"), Environment.ProcessId, null);
        var second = first with { InstanceId = Guid.NewGuid(), Name = "Second" };
        registry.Register(first);
        Dispatcher.UIThread.RunJobs();
        Require(openButton.IsEnabled && form.CanChooseFile && form.SelectedConnector == first,
            "The first registered connector must enable Open without contacting it.");
        registry.Register(second);
        connectorBox.SelectedItem = second;
        Require(form.SelectedConnector == second, "The combo box did not update the selected connector.");
        registry.Register(first with { ProductVersion = "2027" });
        Require(form.SelectedConnector == second, "Refreshing the registry changed the user's selection.");
        registry.Remove(second.InstanceId);
        Require(form.SelectedConnector?.InstanceId == first.InstanceId && form.CanChooseFile,
            "Removing the selected connector must select the remaining connector.");
        registry.Remove(first.InstanceId);
        Dispatcher.UIThread.RunJobs();
        Require(!openButton.IsEnabled && form.SelectedConnector is null,
            "Removing the last connector must disable Open.");
        Require(form.FamilyMetadata.Family is null && form.FileName == "Файл не выбран",
            "Opening the form or changing connectors started reading a file.");
        window.Close();
        registry.Register(first);
        Require(form.Connectors.Count == 0, "A closed form still receives registry updates.");
    }

    private static async Task CheckAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "RevEx-Rfa-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var discovery = new FileConnectorDiscovery(directory);
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
            var firstCsv = Path.Combine(directory, "Sizes.csv");
            var secondCsv = Path.Combine(directory, "Materials.CSV");
            await File.WriteAllTextAsync(firstCsv, "Size,Value\nA,42");
            await File.WriteAllTextAsync(secondCsv, "Material,Code\nSteel,1");
            var originalFirstCsv = await File.ReadAllBytesAsync(firstCsv);
            var originalSecondCsv = await File.ReadAllBytesAsync(secondCsv);
            var ifcPath = Path.Combine(directory, "Geometry.IFC");
            await File.WriteAllTextAsync(ifcPath, "ISO-10303-21;\nHEADER;\nENDSEC;\nEND-ISO-10303-21;");
            var originalIfc = await File.ReadAllBytesAsync(ifcPath);
            using (var unavailable = new RfaFamilyImportViewModel(api, import))
            {
                await unavailable.LoadFileAsync(path);
                Require(!unavailable.CanChooseFile && !unavailable.CanSave && unavailable.FamilyMetadata.Family is null,
                    "Missing connector must block opening and reading.");
            }

            await using var unsupported = new ConnectorHost(new ConnectorHostOptions(
                "Revit without reader", ConnectorProduct.Revit, "2027", DiscoveryDirectory: directory));
            await unsupported.StartAsync();
            var unsupportedDescriptor = (await discovery.FindAsync(default)).Single(
                connector => connector.InstanceId == unsupported.InstanceId);
            registry.Register(unsupportedDescriptor);
            await using var reader = new ConnectorHost(new ConnectorHostOptions(
                "RFA mock reader", ConnectorProduct.Mock, "2026", DiscoveryDirectory: directory));
            var readerExecutor = new ReadExecutor();
            reader.RegisterExecutor(readerExecutor);
            await reader.StartAsync();
            var readerDescriptor = (await discovery.FindAsync(default)).Single(
                connector => connector.InstanceId == reader.InstanceId);
            registry.Register(readerDescriptor);
            using (var invalidSelection = new RfaFamilyImportViewModel(api, import))
            {
                invalidSelection.SelectedConnector = invalidSelection.Connectors.Single(connector => connector.InstanceId == reader.InstanceId);
                await invalidSelection.LoadFilesAsync([firstCsv, secondCsv]);
                Require(invalidSelection.HasError && !invalidSelection.CanSave && readerExecutor.ReadCount == 0,
                    "CSV files alone must not start reading or saving.");
                await invalidSelection.LoadFilesAsync([path, Path.Combine(directory, "Other.rfa")]);
                Require(invalidSelection.HasError && !invalidSelection.CanSave && readerExecutor.ReadCount == 0,
                    "Selecting more than one RFA must not start reading.");
                await invalidSelection.LoadFilesAsync([path, Path.Combine(directory, "Notes.txt")]);
                Require(invalidSelection.HasError && !invalidSelection.CanSave && readerExecutor.ReadCount == 0,
                    "Unsupported auxiliary extensions must be rejected.");
            }
            using (var unsupportedForm = new RfaFamilyImportViewModel(api, import))
            {
                unsupportedForm.SelectedConnector = unsupportedForm.Connectors.Single(connector =>
                    connector.InstanceId == unsupported.InstanceId);
                Require(unsupportedForm.CanChooseFile, "Open must be enabled before checking connector commands.");
                await unsupportedForm.LoadFileAsync(path);
                Require(unsupportedForm.HasError && !unsupportedForm.CanSave && readerExecutor.ReadCount == 0,
                    "An unsupported selection must report an error without falling back to another connector.");
            }
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
                first.SelectedConnector = first.Connectors.Single(connector => connector.InstanceId == reader.InstanceId);
                Require(readerExecutor.ReadCount == 0 && !first.CanSave, "The form started reading before Open.");
                await first.LoadFilesAsync([firstCsv, path, secondCsv, firstCsv, ifcPath]);
                Require(!first.HasError && first.CanSave && first.IsNewContent, first.Message ?? "Reading failed.");
                Require(first.ContentName == name && first.ConnectorName == "RFA mock reader" && readerExecutor.ReadCount == 1,
                    "The selected connector did not read the family exactly once.");
                Require(first.AuxiliaryFileNames == string.Join(Environment.NewLine, "Sizes.csv", "Materials.CSV", "Geometry.IFC"),
                    "The form did not list the selected CSV files or retained duplicate selections.");
                Require(first.FamilyMetadata.Types.Count == 1 && first.Description == "Mock description" && first.ApplicationVersion == "2026", "Form was not populated.");
                Require(first.FamilyMetadata.SelectedType?.Name == "Type A", "Imported family type was not selected.");
                Require(first.FamilyMetadata.Family?.RevitCategory == "Двери" && first.FamilyCategory == "Двери",
                    "The Revit category was not read from a legacy connector.");
                var importedMetadata = JObject.Parse(first.MetadataJson);
                Require(importedMetadata["RevitCategory"]?.Value<string>() == "Двери" && importedMetadata["Category"] is null,
                    "Legacy Category must be normalized to RevitCategory before saving.");
                Require(first.FamilyMetadata.ParameterGroups.Single().GroupId == "PG_GEOMETRY"
                    && first.FamilyMetadata.ParameterGroups.Single().Name == "Размеры", "Parameter groups were lost during import.");
                Require(JObject.Parse(first.MetadataJson)["future"]?["nested"]?[1]?["value"]?.Value<int>() == 42, "Unknown connector fields were lost.");
                first.SelectedCategory = category;
                versions.FailNextUpload = true;
                Require(!await first.SaveAsync(), "Expected an upload failure.");
                Require((await api.ContentVersions.GetAllAsync()).Count == before + 1, "Version was not retained for retry.");
                versions.FailAfterUploadFileName = "Sizes.csv";
                Require(!await first.SaveAsync() && first.CanSave, "Expected a CSV upload interruption with retry available.");
                versions.FailAfterUploadFileName = "Geometry.IFC";
                Require(!await first.SaveAsync() && first.CanSave, "Expected a lost IFC upload response with retry available.");
                Require(await first.SaveAsync(), first.Message ?? "Retry failed.");
                contentId = first.SavedVersion!.ContentId;
                Require(first.SavedVersion.Platform == Platform.Revit && first.SavedVersion.PublishStatus == PublishStatus.Draft,
                    "RFA import must explicitly create a Revit draft even when using a mock connector.");
                Require((await api.ContentVersions.GetAllAsync()).Count == before + 1, "Retry created another version.");
                var uploadedFiles = new List<RevEx.Domain.Contracts.ContentFile.ContentFileDto>();
                foreach (var fileId in first.SavedVersion.FileIds)
                {
                    var uploaded = await api.ContentFiles.GetByIdAsync(fileId);
                    if (uploaded is null)
                    {
                        throw new InvalidOperationException("An uploaded file is missing.");
                    }

                    uploadedFiles.Add(uploaded);
                }

                Require(uploadedFiles.Count == 4 && uploadedFiles.Count(file => file.Role == "primary") == 1,
                    "Retry duplicated the RFA or CSV files.");
                var tables = uploadedFiles.Where(file => file.Role == "lookup-table").ToArray();
                Require(tables.Length == 2 && tables.All(file => file.Metadata is null),
                    "CSV files must be saved as lookup tables without RFA metadata.");
                Require(originalFirstCsv.SequenceEqual(versions.UploadedContents["Sizes.csv"]), "The first CSV changed during import.");
                Require(originalSecondCsv.SequenceEqual(versions.UploadedContents["Materials.CSV"]), "The second CSV changed during import.");
                var ifc = uploadedFiles.Single(file => file.OriginalFileName == "Geometry.IFC");
                Require(ifc.Role == "attachment" && ifc.Metadata is null
                    && originalIfc.SequenceEqual(versions.UploadedContents["Geometry.IFC"]), "IFC was not preserved as an auxiliary attachment.");
                var file = uploadedFiles.Single(file => file.Role == "primary");
                var metadata = JObject.Parse(System.Text.Json.JsonSerializer.Serialize(file.Metadata));
                Require(metadata["RevitCategory"]?.Value<string>() == "Двери" && metadata["Category"] is null,
                    "Saved family metadata still contains the ambiguous Category field.");
                Require(metadata["future"]?["nested"]?[1]?["value"]?.Value<int>() == 42, "Metadata was not saved with the file.");
                var parameter = metadata["Types"]?[0]?["Parameters"]?[0];
                Require(parameter?["GroupId"]?.Value<string>() == "PG_GEOMETRY"
                    && parameter?["GroupName"]?.Value<string>() == "Размеры", "Parameter group metadata was not saved with the file.");
                Require(!first.CanSave, "Completed import can be saved twice.");
            }

            using (var second = new RfaFamilyImportViewModel(api, import))
            {
                readerExecutor.UseLegacyCategory = false;
                second.SelectedConnector = second.Connectors.Single(connector => connector.InstanceId == reader.InstanceId);
                await second.LoadFileAsync(path);
                Require(second.AuxiliaryFileNames == "Вспомогательные файлы не выбраны", "RFA-only import retained auxiliary files.");
                Require(second.FamilyMetadata.Family?.RevitCategory == "Двери", "The current connector category was not read.");
                Require(!second.IsNewContent && !second.CanEditContent, "Existing content was not detected.");
                Require(await second.SaveAsync(), second.Message ?? "Second import failed.");
                Require(second.SavedVersion!.ContentId == contentId, "Second import created a different card.");
                Require(second.SavedVersion.FileIds.Count == 1, "RFA-only import saved unexpected CSV files.");
            }
            Require((await api.ContentVersions.GetAllAsync()).Count == before + 2, "Each completed import must create a new version.");
            Require((await api.Contents.GetAllAsync(new ContentQueryDto { Name = name })).Count == 2, "Duplicate content created.");
            Require(original.SequenceEqual(await File.ReadAllBytesAsync(path)), "Connector modified the user's original file.");
            Require(originalFirstCsv.SequenceEqual(await File.ReadAllBytesAsync(firstCsv))
                && originalSecondCsv.SequenceEqual(await File.ReadAllBytesAsync(secondCsv)), "Import modified the source CSV files.");
            Require(originalIfc.SequenceEqual(await File.ReadAllBytesAsync(ifcPath)), "Import modified the source IFC file.");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
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
        public bool UseLegacyCategory { get; set; } = true;
        public int ReadCount
        {
            get;
            private set;
        }

        public override ConnectorCommand Command => ConnectorCommand.ReadFiles;

        protected override async Task<JObject> ExecuteAsync(ConnecterReadFileRequest request, IConnectorCommandContext context, CancellationToken token)
        {
            ReadCount++;
            var file = request.Files.Single();
            Require(Path.GetExtension(file.Path).Equals(".rfa", StringComparison.OrdinalIgnoreCase),
                "Only the RFA file may be sent to the reader.");
            await File.AppendAllTextAsync(file.Path, "Prepared by connector", token);
            return new JObject
            {
                ["FamilyReadResults"] = new JArray(new JObject
                {
                    ["Name"] = file.Name,
                    ["Path"] = file.Path,
                    ["Description"] = "Mock description",
                    [UseLegacyCategory ? "Category" : "RevitCategory"] = "Двери",
                    ["ApplicationVersion"] = "2026",
                    ["Types"] = new JArray(new JObject
                    {
                        ["Name"] = "Type A",
                        ["Parameters"] = new JArray(new JObject
                        {
                            ["Name"] = "RevEx_Ширина",
                            ["Value"] = "2.6246719160104988",
                            ["StorageType"] = "Double",
                            ["GroupId"] = "PG_GEOMETRY",
                            ["GroupName"] = "Размеры"
                        })
                    }),
                    ["future"] = new JObject { ["nested"] = new JArray("unknown", new JObject { ["value"] = 42 }) }
                })
            };
        }
    }

    private sealed class RetryVersionRepository(IContentVersionRepository inner) : IContentVersionRepository
    {
        public bool FailNextUpload { get; set; }
        public Dictionary<string, byte[]> UploadedContents { get; } = new();
        public string? FailAfterUploadFileName
        {
            get;
            set;
        }

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

        public Task UpdatePublishStatusAsync(int versionId, PublishStatus publishStatus, CancellationToken cancellationToken = default)
        {
            return inner.UpdatePublishStatusAsync(versionId, publishStatus, cancellationToken);
        }

        public async Task<ContentVersionDto> UploadFileAsync(int versionId, Stream content, string fileName, string role,
            CancellationToken cancellationToken = default, string? metadataJson = null)
        {
            if (FailNextUpload)
            {
                FailNextUpload = false;
                throw new HttpRequestException("Simulated upload interruption.");
            }

            await using var captured = new MemoryStream();
            await content.CopyToAsync(captured, cancellationToken);
            UploadedContents[fileName] = captured.ToArray();
            captured.Position = 0;
            var version = await inner.UploadFileAsync(versionId, captured, fileName, role, cancellationToken, metadataJson);
            if (fileName == FailAfterUploadFileName)
            {
                FailAfterUploadFileName = null;
                throw new HttpRequestException("Simulated lost response after CSV upload.");
            }

            return version;
        }

        public Task DeleteFileAsync(int versionId, int fileId, CancellationToken cancellationToken = default)
        {
            return inner.DeleteFileAsync(versionId, fileId, cancellationToken);
        }
    }
}

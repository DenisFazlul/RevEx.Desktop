using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RevEx.Connector.Client;
using RevEx.Connector.Protocol;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.DI;
using RevEx.Desktop.Updates;
using RevEx.Desktop.Services;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Identifiers;

if (args.Length != 2 || !int.TryParse(args[1], out var connectorPort))
{
    Console.WriteLine("Usage: RfaImportSmoke <existing-rfa-file> <mock-connector-port>");
    return;
}

var path = Path.GetFullPath(args[0]);
DesktopUpdates.Initialize();
var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
    .Build();
var services = new ServiceCollection();
services.AddServices(configuration);
await using var provider = services.BuildServiceProvider();
var discovery = provider.GetRequiredService<IConnectorDiscovery>();
var descriptor = (await discovery.FindAsync(default)).Single(connector =>
    connector.Product == ConnectorProduct.Mock && connector.BaseAddress.Port == connectorPort);
var registry = provider.GetRequiredService<IConnectorRegistry>();
registry.Register(descriptor);
// Exercise the regular OIDC flow and HTTP repositories against the running backend.
await provider.GetRequiredService<IAuthenticationService>().GetAccessTokenAsync();
var api = provider.GetRequiredService<IRevExApiService>();
var importer = provider.GetRequiredService<RfaFamilyImportService>();
var existingVersions = await api.ContentVersions.GetAllAsync();
int? contentId = null;
ContentDto? targetContent = null;
for (var addition = 1; addition <= 2; addition++)
{
    using var form = new RfaFamilyImportViewModel(api, importer, targetContent);
    form.SelectedConnector = form.Connectors.Single(connector => connector.InstanceId == descriptor.InstanceId);
    await form.LoadFileAsync(path);
    if (form.HasError || !form.CanSave)
    {
        throw new InvalidOperationException(form.Message);
    }

    form.SelectedCategory ??= form.Categories.FirstOrDefault();
    if (!await form.SaveAsync())
    {
        throw new InvalidOperationException(form.Message);
    }

    var version = form.SavedVersion!;
    contentId ??= version.ContentId;
    targetContent ??= await api.Contents.GetByIdAsync(version.ContentId);
    if (version.ContentId != contentId)
    {
        throw new InvalidOperationException("Repeated import created a different content card.");
    }

    var fileId = version.FileIds.Single();
    var file = await api.ContentFiles.GetByIdAsync(fileId);
    if (file?.Metadata is null || file.Extension != ".rfa")
    {
        throw new InvalidOperationException("The uploaded file has no metadata.");
    }

    var downloaded = await api.ContentFiles.DownloadAsync(fileId);
    var preparedText = Encoding.UTF8.GetString(downloaded);
    if (!Uuid7.IsValid(version.Vid ?? Guid.Empty) ||
        targetContent is null ||
        !preparedText.Contains($"RevEx_CID={targetContent.Cid:D}", StringComparison.Ordinal) ||
        !preparedText.Contains($"RevEx_VID={version.Vid:D}", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("The stored RFA does not contain the catalog CID and VID written by the mock.");
    }

    var source = await File.ReadAllBytesAsync(path);
    if (!source.SequenceEqual(downloaded))
    {
        throw new InvalidOperationException("The stored RFA differs from the user's source file.");
    }

    Console.WriteLine($"PASS: addition {addition}, content {contentId}, version {version.Id}, file {fileId}, bytes {downloaded.Length}, metadata returned by HTTP.");
}

var matching = await api.Contents.GetAllAsync(new ContentQueryDto { Name = Path.GetFileNameWithoutExtension(path) });
if (matching.Count(content => content.Id == contentId) != 1 ||
    (await api.ContentVersions.GetAllAsync()).Count(version => version.ContentId == contentId) !=
    existingVersions.Count(version => version.ContentId == contentId) + 2)
{
    throw new InvalidOperationException("Expected one content card with two new versions.");
}

Console.WriteLine("PASS: running Desktop import model -> external mock TCP connector -> authenticated HTTP API -> file storage; one card, two new versions, source RFA updated.");

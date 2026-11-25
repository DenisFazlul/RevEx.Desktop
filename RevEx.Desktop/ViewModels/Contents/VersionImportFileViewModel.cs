using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevEx.Domain.Contracts.Connectors;

namespace RevEx.Desktop.ViewModels.Contents;

public sealed class VersionImportFileViewModel
{
    public VersionImportFileViewModel(ConnectorFileMetadata file)
    {
        Name = file.File.Name;
        try
        {
            Metadata = JToken.Parse(file.MetadataJson).ToString(Formatting.Indented);
        }
        catch (JsonException)
        {
            Metadata = file.MetadataJson;
        }
    }

    public string Name { get; }

    public string Metadata { get; }
}

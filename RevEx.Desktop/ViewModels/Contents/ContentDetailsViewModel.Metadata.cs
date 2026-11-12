using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevEx.Desktop.ViewModels.Revit;
using RevEx.Domain.Contracts.ContentFile;
using RevEx.Domain.Contracts.Revit;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentDetailsViewModel
{
    private ContentFileDto[] _versionMetadataFiles = [];
    public RevitFamilyReadResultViewModel VersionFamilyMetadata { get; } = new();

    public bool IsRevitVersion => string.Equals(SelectedVersion?.Platform.ToString(), "Revit", StringComparison.OrdinalIgnoreCase);

    public string VersionMetadataJson { get; private set; } = string.Empty;

    public string VersionMetadataMessage { get; private set; } = string.Empty;

    public bool HasGenericVersionMetadata => !IsRevitVersion && !string.IsNullOrEmpty(VersionMetadataJson);

    private void UpdateVersionMetadata(IEnumerable<ContentFileDto> files)
    {
        _versionMetadataFiles = files.ToArray();
        VersionFamilyMetadata.Family = null;
        VersionMetadataJson = string.Empty;
        VersionMetadataMessage = string.Empty;
        var file = _versionMetadataFiles.Where(item => item.Metadata is not null)
            .OrderByDescending(item => item.Role == "primary")
            .ThenBy(item => item.Id)
            .FirstOrDefault();

        if (file?.Metadata is not null)
        {
            try
            {
                var metadata = file.Metadata is JToken token
                    ? token
                    : JToken.Parse(file.Metadata is System.Text.Json.JsonElement element
                        ? element.GetRawText()
                        : JsonConvert.SerializeObject(file.Metadata));
                VersionMetadataJson = metadata.ToString(Formatting.Indented);
                if (IsRevitVersion)
                {
                    VersionFamilyMetadata.Family = metadata.ToObject<RevitFamilyReadResult>();
                }
            }
            catch (JsonException)
            {
                VersionMetadataMessage = "Не удалось прочитать метаданные файла версии.";
            }
        }
        else if (!IsRevitVersion)
        {
            VersionMetadataMessage = "У файлов этой версии нет метаданных.";
        }

        OnPropertyChanged(nameof(IsRevitVersion));
        OnPropertyChanged(nameof(VersionMetadataJson));
        OnPropertyChanged(nameof(VersionMetadataMessage));
        OnPropertyChanged(nameof(HasGenericVersionMetadata));
    }
}

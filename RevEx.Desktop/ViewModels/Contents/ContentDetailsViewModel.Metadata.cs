using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevEx.Desktop.ViewModels.Revit;
using RevEx.Domain.Contracts.ContentFile;
using RevEx.Domain.Contracts.Revit;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentDetailsViewModel
{
    private ContentFileDto[] _versionMetadataFiles = [];
    public RevitFamilyReadResultViewModel VersionFamilyMetadata { get; } = new();
    public OtherFileMetadataViewModel OtherFileMetadata { get; } = new();

    public bool IsRevitVersion => SelectedVersion?.Platform == Platform.Revit;

    public bool IsOtherVersion => SelectedVersion?.Platform == Platform.Other;

    public string VersionMetadataJson { get; private set; } = string.Empty;

    public string VersionMetadataMessage { get; private set; } = string.Empty;

    public bool HasGenericVersionMetadata => !IsRevitVersion && !IsOtherVersion
        && !string.IsNullOrEmpty(VersionMetadataJson);

    private void UpdateVersionMetadata(IEnumerable<ContentFileDto> files)
    {
        _versionMetadataFiles = files.ToArray();
        VersionFamilyMetadata.Family = null;
        OtherFileMetadata.SetMetadata(null);
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
                else if (IsOtherVersion)
                {
                    OtherFileMetadata.SetMetadata(metadata);
                }
            }
            catch (JsonException)
            {
                VersionMetadataMessage = "Не удалось прочитать метаданные файла версии.";
            }
        }
        else if (!IsRevitVersion && !IsOtherVersion)
        {
            VersionMetadataMessage = "У файлов этой версии нет метаданных.";
        }

        OnPropertyChanged(nameof(IsRevitVersion));
        OnPropertyChanged(nameof(IsOtherVersion));
        OnPropertyChanged(nameof(VersionMetadataJson));
        OnPropertyChanged(nameof(VersionMetadataMessage));
        OnPropertyChanged(nameof(HasGenericVersionMetadata));
    }
}

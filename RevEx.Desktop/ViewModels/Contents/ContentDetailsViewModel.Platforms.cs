using System.Collections.Generic;
using System.Linq;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.ViewModels.Contents;

public sealed record VersionPlatformOption(Platform Value, string Name);

public partial class ContentDetailsViewModel
{

    public IReadOnlyList<VersionPlatformOption> VersionPlatforms { get; } =
    [
        new(Platform.Revit, "Revit"),
        new(Platform.AutoCad, "AutoCAD"),
        new(Platform.Other, "Другое")
    ];

    private VersionPlatformOption? _selectedVersionPlatform;

    public VersionPlatformOption? SelectedVersionPlatform
    {
        get => _selectedVersionPlatform;
        set
        {
            if (value is null || IsVersionBusy || !SetProperty(ref _selectedVersionPlatform, value))
            {
                return;
            }

            NotifyPlatformVersionsChanged();
            var version = FilteredVersions.FirstOrDefault();
            if (version is not null)
            {
                _ = SelectVersionAsync(version);
            }
            else
            {
                SelectedVersion = null;
                VersionFiles.Clear();
                UpdateVersionMetadata(System.Array.Empty<RevEx.Domain.Contracts.ContentFile.ContentFileDto>());
                NotifyVersionStateChanged();
            }
        }
    }

    public IReadOnlyList<ContentVersionDto> FilteredVersions => Versions
        .Where(version => version.Platform == _selectedVersionPlatform?.Value)
        .ToArray();

    public bool HasNoPlatformVersions => !IsLoading && FilteredVersions.Count == 0;

    private void SynchronizeVersionPlatform(ContentVersionDto? version)
    {
        if (version is not null)
        {
            _selectedVersionPlatform = VersionPlatforms.FirstOrDefault(option => option.Value == version.Platform);
            OnPropertyChanged(nameof(SelectedVersionPlatform));
        }
        NotifyPlatformVersionsChanged();
    }

    private void NotifyPlatformVersionsChanged()
    {
        OnPropertyChanged(nameof(FilteredVersions));
        OnPropertyChanged(nameof(HasNoPlatformVersions));
    }
}

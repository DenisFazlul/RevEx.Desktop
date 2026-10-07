namespace RevEx.Desktop.Core.Domain;

public sealed record DesktopReleaseDto(string BackendVersion, string RecommendedDesktopVersion,
    string MinimumDesktopVersion, string MaximumDesktopVersion, string UpdatesPath,
    Dictionary<string, string> Installers, string Notes);

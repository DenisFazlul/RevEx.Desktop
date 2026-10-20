namespace RevEx.Desktop.Updates;

public sealed record DesktopPackage(string Version, string Platform, string FileName, long Size, string Sha256, string Sha1);
public sealed record DesktopUpdateResponse(string Decision, string TargetVersion, string Notes,
    DesktopPackage? Package, bool IsDowngrade, Guid? OfferId, string? DownloadPath);
public sealed record DesktopInstallationRequest(Guid InstallationId, string InstallationKey, string Version,
    string Platform, Guid? CompletedOfferId);

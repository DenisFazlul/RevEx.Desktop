using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public class AppSettings : IAppSettings
{
    public string ApiPath { get; set; } = string.Empty;
    public AuthenticationSettings Authentication { get; set; } = new();
}

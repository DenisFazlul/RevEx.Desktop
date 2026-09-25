using RevEx.Desktop.Core.Services;

namespace RevEx.Desktop.Core.Interfaces;

public interface IAppSettings
{
    string ApiPath { get; set; }
    string ConnectorRegistrationAddress { get; set; }
    AuthenticationSettings Authentication { get; set; }
}

using RevEx.Connector.Contracts;
using RevEx.Configuration;

namespace RevEx.Connector.Host;

public sealed record ConnectorHostOptions
{
    public static readonly Uri DefaultDesktopRegistrationAddress =
        ConnectorRegistrationSettings.LoadFromDesktopUserSettings();

    public ConnectorHostOptions(
        string name,
        ConnectorProduct product,
        string productVersion,
        Uri desktopRegistrationAddress,
        string? activeDocument = null,
        Guid? instanceId = null)
    {
        Name = name;
        Product = product;
        ProductVersion = productVersion;
        DesktopRegistrationAddress = ValidateAddress(desktopRegistrationAddress);
        ActiveDocument = activeDocument;
        InstanceId = instanceId;
    }

    public string Name { get; }
    public ConnectorProduct Product { get; }
    public string ProductVersion { get; }
    public Uri DesktopRegistrationAddress { get; }
    public string? ActiveDocument { get; }
    public Guid? InstanceId { get; }

    private static Uri ValidateAddress(Uri address)
    {
        if (!address.IsAbsoluteUri ||
            address.Scheme != Uri.UriSchemeHttp ||
            !address.IsLoopback)
        {
            throw new ArgumentException(
                "Адрес регистрации Desktop должен быть абсолютным loopback HTTP-адресом.",
                nameof(address));
        }

        return address;
    }
}

namespace RevEx.Configuration;

public static class ConnectorRegistrationSettings
{
    public const string DefaultAddress = "http://127.0.0.1:5060/";

    public static Uri LoadFromDesktopUserSettings()
    {
        var userAddress = new RevExUserSettingsStore().Load().ConnectorRegistrationAddress;
        var effectiveAddress = string.IsNullOrWhiteSpace(userAddress) ? DefaultAddress : userAddress;

        if (!Uri.TryCreate(effectiveAddress, UriKind.Absolute, out var address) ||
            address.Scheme != Uri.UriSchemeHttp ||
            !address.IsLoopback)
        {
            throw new InvalidOperationException(
                "ConnectorRegistrationAddress должен содержать loopback HTTP-адрес.");
        }

        return address;
    }
}

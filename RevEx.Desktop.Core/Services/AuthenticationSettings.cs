namespace RevEx.Desktop.Core.Services;

public sealed class AuthenticationSettings
{
    public string Provider { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public Dictionary<string, OidcProviderSettings> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class OidcProviderSettings
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string? RefreshTokenRevocationEndpoint { get; set; }
}

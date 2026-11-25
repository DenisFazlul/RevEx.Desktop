using System.Net;
using System.Text;
using System.Text.Json;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Authentication;

public sealed class OidcAuthenticationService : IAuthenticationService
{
    private const string AuthorizationPageResource =
        "RevEx.Desktop.Core.Authentication.Assets.AuthorizationCompleted.html";
    private const string AuthorizationMascotResource =
        "RevEx.Desktop.Core.Authentication.Assets.Tyubik.png";

    private readonly OidcClient _oidcClient;
    private readonly HttpClient _backchannelClient;
    private readonly IAuthenticationBrowser _authenticationBrowser;
    private readonly string _clientId;
    private readonly string? _refreshTokenRevocationEndpoint;
    private readonly string _redirectUri;
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _accessTokenExpiration;
    private IReadOnlySet<string> _roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlySet<string> Roles => _roles;

    public bool IsInRole(string role) => _roles.Contains(role);

    public OidcAuthenticationService(
        IAppSettings settings,
        IHttpClientFactory httpClientFactory,
        IAuthenticationBrowser authenticationBrowser)
    {
        var authentication = settings.Authentication;
        if (!authentication.Providers.TryGetValue(authentication.Provider, out var provider))
            throw new InvalidOperationException(
                $"Authentication provider '{authentication.Provider}' is not configured.");

        _backchannelClient = httpClientFactory.CreateClient(nameof(OidcAuthenticationService));
        _authenticationBrowser = authenticationBrowser;
        _clientId = provider.ClientId;
        _refreshTokenRevocationEndpoint = provider.RefreshTokenRevocationEndpoint;
        _redirectUri = authentication.RedirectUri;
        _oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = provider.Authority,
            ClientId = provider.ClientId,
            Scope = provider.Scope,
            RedirectUri = authentication.RedirectUri,
            DisablePushedAuthorization = true,
            Policy = new Policy
            {
                Discovery = new DiscoveryPolicy
                {
                    RequireHttps = provider.RequireHttpsMetadata
                }
            }
        });
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasValidAccessToken())
            return _accessToken!;

        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            if (HasValidAccessToken())
                return _accessToken!;

            if (_refreshToken is not null && await TryRefreshTokenAsync(cancellationToken))
                return _accessToken!;

            _accessToken = await LoginAsync(cancellationToken);
            return _accessToken;
        }
        finally
        {
            _loginLock.Release();
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            var refreshToken = _refreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken) ||
                string.IsNullOrWhiteSpace(_refreshTokenRevocationEndpoint))
            {
                ClearTokens();
                return;
            }

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["refresh_token"] = refreshToken
            });
            using var response = await _backchannelClient.PostAsync(
                _refreshTokenRevocationEndpoint,
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"OIDC refresh token revocation failed ({(int)response.StatusCode}): {details}");
            }

        }
        finally
        {
            ClearTokens();
            _loginLock.Release();
        }
    }

    private bool HasValidAccessToken() =>
        _accessToken is not null && _accessTokenExpiration > DateTimeOffset.UtcNow.AddMinutes(1);

    private void ClearTokens()
    {
        _accessToken = null;
        _refreshToken = null;
        _accessTokenExpiration = default;
        _roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        var result = await _oidcClient.RefreshTokenAsync(
            _refreshToken!,
            cancellationToken: cancellationToken);

        if (result.IsError || string.IsNullOrWhiteSpace(result.AccessToken))
        {
            _accessToken = null;
            _refreshToken = null;
            return false;
        }

        _accessToken = result.AccessToken;
        _roles = ReadRoles(result.AccessToken);
        _refreshToken = result.RefreshToken ?? _refreshToken;
        _accessTokenExpiration = result.AccessTokenExpiration;
        return true;
    }

    private async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        var state = await _oidcClient.PrepareLoginAsync(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(state.StartUrl))
            throw new InvalidOperationException("The identity provider returned an empty authorization URL.");

        using var listener = CreateCallbackListener();
        try
        {
            var authorizationUri = new Uri(state.StartUrl, UriKind.Absolute);
            var expectedState = GetQueryParameter(authorizationUri.Query, "state")
                ?? throw new InvalidOperationException("The authorization URL does not contain state.");
            _authenticationBrowser.Open(authorizationUri);

            var context = await WaitForCallbackAsync(listener, expectedState, cancellationToken);
            var result = await _oidcClient.ProcessResponseAsync(
                context.Request.RawUrl,
                state,
                cancellationToken: cancellationToken);

            if (result.IsError || string.IsNullOrWhiteSpace(result.AccessToken))
            {
                await WriteBrowserErrorResponseAsync(context.Response, result.Error);
                throw new InvalidOperationException($"OIDC authentication failed: {result.Error}");
            }

            await WriteBrowserResponseAsync(context.Response, CancellationToken.None);

            _refreshToken = result.RefreshToken;
            _accessTokenExpiration = result.AccessTokenExpiration;
            _roles = ReadRoles(result.AccessToken);
            return result.AccessToken;
        }
        finally
        {
            if (listener.IsListening)
                listener.Stop();
        }
    }

    private static async Task<HttpListenerContext> WaitForCallbackAsync(
        HttpListener listener,
        string expectedState,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var context = await listener.GetContextAsync().WaitAsync(cancellationToken);
            var receivedState = context.Request.QueryString["state"];
            if (string.Equals(receivedState, expectedState, StringComparison.Ordinal))
            {
                return context;
            }

            await WriteBrowserErrorResponseAsync(
                context.Response,
                "Эта ссылка относится к другой или уже завершённой попытке входа.");
        }
    }

    private static string? GetQueryParameter(string query, string name)
    {
        var parameters = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var parameter in parameters)
        {
            var parts = parameter.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0]).Equals(name, StringComparison.Ordinal))
            {
                return parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            }
        }

        return null;
    }

    private HttpListener CreateCallbackListener()
    {
        var listener = new HttpListener
        {
            IgnoreWriteExceptions = true
        };
        listener.Prefixes.Add(_redirectUri);

        try
        {
            listener.Start();
            return listener;
        }
        catch (HttpListenerException exception)
        {
            listener.Close();
            throw new InvalidOperationException(
                $"Не удалось открыть адрес авторизации {_redirectUri}. " +
                "Порт занят другим приложением. Закройте второй экземпляр RevEx и повторите попытку.",
                exception);
        }
    }

    private static async Task WriteBrowserResponseAsync(
        HttpListenerResponse response,
        CancellationToken cancellationToken)
    {
        var html = LoadAuthorizationPage();
        var content = Encoding.UTF8.GetBytes(html);

        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = content.Length;
        await response.OutputStream.WriteAsync(content, cancellationToken);
        response.Close();
    }

    private static async Task WriteBrowserErrorResponseAsync(
        HttpListenerResponse response,
        string? error)
    {
        var details = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(error) ? "Авторизация не завершена." : error);
        var html = $"""
            <!doctype html>
            <html lang="ru">
            <head><meta charset="utf-8"><title>Авторизация не завершена</title></head>
            <body style="font-family: sans-serif; margin: 40px; color: #202124">
                <h1>Авторизация не завершена</h1>
                <p>{details}</p>
                <p>Вернитесь в приложение RevEx и повторите вход.</p>
            </body>
            </html>
            """;
        var content = Encoding.UTF8.GetBytes(html);

        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = content.Length;
        await response.OutputStream.WriteAsync(content, CancellationToken.None);
        response.Close();
    }

    private static string LoadAuthorizationPage()
    {
        using var stream = typeof(OidcAuthenticationService).Assembly
            .GetManifestResourceStream(AuthorizationPageResource)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{AuthorizationPageResource}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);

        using var mascot = typeof(OidcAuthenticationService).Assembly
            .GetManifestResourceStream(AuthorizationMascotResource)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{AuthorizationMascotResource}' was not found.");
        using var image = new MemoryStream();
        mascot.CopyTo(image);
        var base64Image = Convert.ToBase64String(image.ToArray());
        return reader.ReadToEnd().Replace("{{TYUBIK_PNG}}", base64Image, StringComparison.Ordinal);
    }

    private static IReadOnlySet<string> ReadRoles(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length != 3)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (!document.RootElement.TryGetProperty("roles", out var roles) ||
                roles.ValueKind != JsonValueKind.Array)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return roles.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

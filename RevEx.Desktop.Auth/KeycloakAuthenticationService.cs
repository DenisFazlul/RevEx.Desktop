using System.Diagnostics;
using System.Net;
using System.Text;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Auth;

public sealed class KeycloakAuthenticationService : IAuthenticationService
{
    private const string AuthorizationPageResource =
        "RevEx.Desktop.Auth.Assets.AuthorizationCompleted.html";

    private readonly OidcClient _oidcClient;
    private readonly HttpClient _backchannelClient;
    private readonly string _clientId;
    private readonly string _logoutEndpoint;
    private readonly string _redirectUri;
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _accessTokenExpiration;

    public KeycloakAuthenticationService(
        IAppSettings settings,
        IHttpClientFactory httpClientFactory)
    {
        var authentication = settings.Authentication;
        _backchannelClient = httpClientFactory.CreateClient(nameof(KeycloakAuthenticationService));
        _clientId = authentication.ClientId;
        _logoutEndpoint = $"{authentication.Authority.TrimEnd('/')}/protocol/openid-connect/logout";
        _redirectUri = authentication.RedirectUri;
        _oidcClient = new OidcClient(new OidcClientOptions
        {
            Authority = authentication.Authority,
            ClientId = authentication.ClientId,
            Scope = authentication.Scope,
            RedirectUri = authentication.RedirectUri,
            DisablePushedAuthorization = true,
            Policy = new Policy
            {
                Discovery = new DiscoveryPolicy
                {
                    RequireHttps = authentication.RequireHttpsMetadata
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

            if (string.IsNullOrWhiteSpace(refreshToken))
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
                _logoutEndpoint,
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Keycloak logout failed ({(int)response.StatusCode}): {details}");
            }

            ClearTokens();
        }
        finally
        {
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
        _refreshToken = result.RefreshToken ?? _refreshToken;
        _accessTokenExpiration = result.AccessTokenExpiration;
        return true;
    }

    private async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        var state = await _oidcClient.PrepareLoginAsync(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(state.StartUrl))
            throw new InvalidOperationException("Keycloak returned an empty authorization URL.");

        using var listener = CreateCallbackListener();
        try
        {
            Process.Start(new ProcessStartInfo(state.StartUrl) { UseShellExecute = true });

            var context = await listener.GetContextAsync().WaitAsync(cancellationToken);
            var result = await _oidcClient.ProcessResponseAsync(
                context.Request.RawUrl,
                state,
                cancellationToken: cancellationToken);

            if (result.IsError || string.IsNullOrWhiteSpace(result.AccessToken))
                throw new InvalidOperationException($"Keycloak authentication failed: {result.Error}");

            await WriteBrowserResponseAsync(context.Response, cancellationToken);

            _refreshToken = result.RefreshToken;
            _accessTokenExpiration = result.AccessTokenExpiration;
            return result.AccessToken;
        }
        finally
        {
            if (listener.IsListening)
                listener.Stop();
        }
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

    private static string LoadAuthorizationPage()
    {
        using var stream = typeof(KeycloakAuthenticationService).Assembly
            .GetManifestResourceStream(AuthorizationPageResource)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{AuthorizationPageResource}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}

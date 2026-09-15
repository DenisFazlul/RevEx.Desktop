using Microsoft.Extensions.DependencyInjection;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;

namespace RevEx.Desktop.Auth;

public static class AuthenticationServiceExtensions
{
    public static IHttpClientBuilder AddConfiguredAuthentication(
        this IHttpClientBuilder builder,
        AuthenticationSettings settings)
    {
        if (settings.Provider.Equals(
                AuthenticationProviderNames.ActiveDirectory,
                StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddSingleton<IAuthenticationService, WindowsAuthenticationService>();
            return builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseDefaultCredentials = true
            });
        }

        builder.Services.AddSingleton<IAuthenticationService, OidcAuthenticationService>();
        builder.Services.AddTransient<AuthenticationHandler>();

        return builder.AddHttpMessageHandler<AuthenticationHandler>();
    }
}

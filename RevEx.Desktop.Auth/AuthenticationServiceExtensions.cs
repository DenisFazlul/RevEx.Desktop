using Microsoft.Extensions.DependencyInjection;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Auth;

public static class AuthenticationServiceExtensions
{
    public static IHttpClientBuilder AddKeycloakAuthentication(this IHttpClientBuilder builder)
    {
        builder.Services.AddSingleton<IAuthenticationService, KeycloakAuthenticationService>();
        builder.Services.AddTransient<AuthenticationHandler>();

        return builder.AddHttpMessageHandler<AuthenticationHandler>();
    }
}

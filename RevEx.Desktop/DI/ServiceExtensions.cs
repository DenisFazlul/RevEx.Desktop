using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RevEx.Desktop.Auth;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.Services;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.Views;
using RevEx.Desktop.Views.MainMenu;
using RevEx.Connector.Contracts;
using RevEx.Desktop.Connectors;
using RevEx.Configuration;
using RevEx.LoadingQueue;
using RevEx.Desktop.Updates;

namespace RevEx.Desktop.DI;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var appSettings = GetAppSettings(configuration);

        services.AddSingleton<AppSettings>(appSettings);
        services.AddSingleton<IAppSettings>(appSettings);
        services.AddDesktopUpdates();
        services.AddRevExtApiConnection(appSettings);
        services.AddSingleton<IConnectorRegistry, ConnectorRegistry>();
        services.AddSingleton<IFileLoadingQueue, FileLoadingQueue>();
        services.AddSingleton(provider => new DesktopConnectorServer(
            provider.GetRequiredService<IConnectorRegistry>(),
            provider.GetRequiredService<IFileLoadingQueue>(),
            new Uri(appSettings.ConnectorRegistrationAddress)));
        services.AddHttpClient<IConnectorClient, ConnectorClient>(httpClient =>
            httpClient.Timeout = TimeSpan.FromMinutes(2));

        services.AddUIServices();
        return services;
    }

    private static AppSettings GetAppSettings(IConfiguration configuration)
    {
        var settings = configuration
            .GetRequiredSection("AppSettings")
            .Get<AppSettings>()
            ?? throw new InvalidOperationException("Не удалось загрузить настройки API.");

        if (!Uri.TryCreate(settings.ApiPath, UriKind.Absolute, out var apiUri) ||
            (apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Настройка AppSettings:ApiPath должна содержать абсолютный HTTP(S)-адрес.");
        }

        if (!Uri.TryCreate(
                settings.ConnectorRegistrationAddress,
                UriKind.Absolute,
                out var connectorRegistrationUri) ||
            connectorRegistrationUri.Scheme != Uri.UriSchemeHttp ||
            !connectorRegistrationUri.IsLoopback)
        {
            throw new InvalidOperationException(
                "AppSettings:ConnectorRegistrationAddress должен содержать loopback HTTP-адрес.");
        }

        ValidateAuthenticationSettings(settings.Authentication);

        return settings;
    }

    private static void ValidateAuthenticationSettings(AuthenticationSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Provider))
            throw new InvalidOperationException("AppSettings:Authentication:Provider is required.");

        if (settings.Provider.Equals(
                AuthenticationProviderNames.ActiveDirectory,
                StringComparison.OrdinalIgnoreCase))
            return;

        if (!settings.Provider.Equals(
                AuthenticationProviderNames.Keycloak,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Authentication provider '{settings.Provider}' is not supported. " +
                $"Supported providers: {AuthenticationProviderNames.Keycloak}, " +
                $"{AuthenticationProviderNames.ActiveDirectory}.");
        }

        if (!settings.Providers.TryGetValue(AuthenticationProviderNames.Keycloak, out var provider))
            throw new InvalidOperationException("Keycloak authentication is not configured.");
        if (!Uri.TryCreate(provider.Authority, UriKind.Absolute, out var authorityUri) ||
            (provider.RequireHttpsMetadata && authorityUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"AppSettings:Authentication:Providers:{settings.Provider}:Authority is invalid.");
        }
        if (string.IsNullOrWhiteSpace(provider.ClientId))
            throw new InvalidOperationException("Authentication provider ClientId is required.");
        if (string.IsNullOrWhiteSpace(provider.Scope))
            throw new InvalidOperationException("Authentication provider Scope is required.");
        if (!Uri.TryCreate(settings.RedirectUri, UriKind.Absolute, out var redirectUri) ||
            redirectUri.Host != "127.0.0.1")
        {
            throw new InvalidOperationException(
                "AppSettings:Authentication:RedirectUri must use the 127.0.0.1 loopback address.");
        }
    }

    private static IHttpClientBuilder AddRevExtApiConnection(
        this IServiceCollection services,
        AppSettings appSettings) =>
        services
            .AddHttpClient<IRevExApiService, RevExApiService>(httpClient =>
            {
                httpClient.BaseAddress = new Uri(appSettings.ApiPath);
                if (DesktopUpdates.InstalledVersion is { } version)
                {
                    httpClient.DefaultRequestHeaders.Add("X-RevEx-Desktop-Version", version);
                    httpClient.DefaultRequestHeaders.Add("X-RevEx-Desktop-Platform", DesktopUpdates.Platform);
                }
            })
            .AddConfiguredAuthentication(appSettings.Authentication);

    private static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationShutdownService, ApplicationShutdownService>();
        services.AddSingleton<IRevExUserSettingsStore, RevExUserSettingsStore>();
        services.AddSingleton<IMainMenuProvider, MainMenuProvider>();
        services.AddTransient<IWorkspaceTabFactory, WorkspaceTabFactory>();

        services.AddTransient<MainMenuView>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        return services;
    }
}

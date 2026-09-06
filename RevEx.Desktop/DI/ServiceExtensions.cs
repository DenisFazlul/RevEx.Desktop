using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.Views;
using RevEx.Desktop.Views.MainMenu;

namespace RevEx.Desktop.DI;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var appSettings = GetAppSettings(configuration);

        
        services.AddSingleton<IAppSettings>(appSettings);
        services.AddRevExHttpClient(appSettings);

        services.AddMainServices();
        services.AddUIServices();
        

        return services;
    }

    private static AppSettings GetAppSettings(IConfiguration configuration)
    {
        var settings = configuration
            .GetRequiredSection("Api")
            .Get<AppSettings>()
            ?? throw new InvalidOperationException("Не удалось загрузить настройки API.");

        if (!Uri.TryCreate(settings.ApiPath, UriKind.Absolute, out var apiUri) ||
            (apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Настройка Api:ApiPath должна содержать абсолютный HTTP(S)-адрес.");
        }

        return settings;
    }

    private static IHttpClientBuilder AddRevExHttpClient(
        this IServiceCollection services,
        AppSettings appSettings) =>
        services.AddHttpClient<IRevExApiService, RevExApiService>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(appSettings.ApiPath);
        });

    private static IServiceCollection AddMainServices(this IServiceCollection services)
    {
        services.AddTransient<IRevExApiService, MockRevExApiService>();
        return services;
    }

    private static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        services.AddSingleton<IMainMenuProvider, MainMenuProvider>();
        services.AddTransient<IWorkspaceTabFactory, WorkspaceTabFactory>();

        services.AddTransient<MainMenuView>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        return services;
    }
}


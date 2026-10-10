using Microsoft.Extensions.DependencyInjection;
using RevEx.Desktop.Updates.Services;
using Velopack;
using RevEx.Configuration;
using Velopack.Locators;

namespace RevEx.Desktop.Updates;

public static class DesktopUpdates
{
    // Must run before UI initialization and single-instance locking.
    public static void Initialize() => VelopackApp.Build().SetAutoApplyOnStartup(false).Run();

    public static IServiceCollection AddDesktopUpdates(this IServiceCollection services)
    {
        services.AddSingleton(provider => new DesktopInstallationState(Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(provider.GetRequiredService<IRevExUserSettingsStore>().FilePath))!, "desktop-installation.json")));
        services.AddHttpClient<DesktopUpdateService>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddTransient<StartupUpdateCoordinator>();
        return services;
    }

    // Local installation metadata; no update source or network access is needed.
    public static string Platform => DesktopUpdateService.Platform;

    public static string? InstalledVersion => VelopackLocator.Current.CurrentlyInstalledVersion?.ToString();
}

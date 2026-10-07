using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.DI;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.Services;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.Views;
using RevEx.Desktop.Views.MainMenu;
using RevEx.Desktop.Connectors;
using RevEx.Configuration;
using RevEx.Desktop.Services.Updates;

namespace RevEx.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDevTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var configuration = CreateConfig();
        var services = new ServiceCollection();
        
        services.AddServices(configuration);

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Startup += async (_, _) => await StartDesktopAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task StartDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            if (!await Services.GetRequiredService<IStartupUpdateCoordinator>().RunAsync())
                return;

            await Services.GetRequiredService<DesktopConnectorServer>().StartAsync();
            var authenticationService = Services.GetRequiredService<IAuthenticationService>();
            await authenticationService.GetAccessTokenAsync();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"OIDC startup authentication failed: {exception}");

            var errorWindow = new AuthenticationErrorWindow(exception.Message);
            desktop.MainWindow = errorWindow;
            errorWindow.Closed += (_, _) => desktop.Shutdown(1);
            errorWindow.Show();
        }
    }

    private IConfigurationRoot CreateConfig()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile(RevExSettingsPaths.UserSettingsFile, optional: true)
            .Build();
        return configuration;
    }
}

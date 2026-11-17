using System;
using System.Threading.Tasks;
using System.Threading;
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
using RevEx.Connector.Client;
using RevEx.Configuration;
using RevEx.Desktop.Updates.Services;

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
            var shutdownCompleted = false;
            var shutdownStarted = false;
            desktop.ShutdownRequested += async (_, e) =>
            {
                if (shutdownCompleted)
                {
                    return;
                }

                e.Cancel = true;
                if (shutdownStarted)
                {
                    return;
                }

                shutdownStarted = true;
                try
                {
                    using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await Services.GetRequiredService<DesktopControlBridge>().StopAsync(timeout.Token)
                        .WaitAsync(timeout.Token);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine($"Ошибка остановки Desktop control bridge: {error}");
                }

                try
                {
                    using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await Services.GetRequiredService<IConnectorRuntime>().StopAsync(timeout.Token)
                        .WaitAsync(timeout.Token);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine($"Ошибка остановки коннекторов: {error}");
                }
                finally
                {
                    (Services.GetRequiredService<RevEx.Connector.Contracts.IConnectorClient>() as IDisposable)?.Dispose();
                    shutdownCompleted = true;
                    desktop.Shutdown();
                }
            };
            desktop.Startup += async (_, _) => await StartDesktopAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task StartDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        AuthenticationProgressWindow? authenticationWindow = null;
        try
        {
            if (!await Services.GetRequiredService<StartupUpdateCoordinator>().RunAsync())
            {
                desktop.Shutdown();
                return;
            }
            authenticationWindow = new AuthenticationProgressWindow(
                Services.GetRequiredService<IAuthenticationBrowser>());
            desktop.MainWindow = authenticationWindow;
            authenticationWindow.Show();

            using var authenticationTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            using var authenticationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                authenticationTimeout.Token,
                authenticationWindow.CancellationToken);
            var authenticationService = Services.GetRequiredService<IAuthenticationService>();
            var accessToken = await authenticationService.GetAccessTokenAsync(
                authenticationCancellation.Token);
            authenticationWindow.Complete();
            authenticationWindow = null;

            await Services.GetRequiredService<IConnectorRuntime>().StartAsync();
            await Services.GetRequiredService<DesktopControlBridge>().StartAsync();
            try
            {
                using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                await Services.GetRequiredService<DesktopUpdateService>().ReportAuthenticatedAsync(
                    Services.GetRequiredService<IAppSettings>().ApiPath, accessToken, timeout.Token);
            }
            catch (Exception trackingError)
            {
                // Retry on the next startup. Telemetry failure must not prevent authenticated work.
                Console.Error.WriteLine($"Не удалось подтвердить версию десктопа: {trackingError.Message}");
            }

            var mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            mainWindow.Show();
        }
        catch (OperationCanceledException) when (
            authenticationWindow?.CancellationToken.IsCancellationRequested == true)
        {
            desktop.Shutdown();
        }
        catch (OperationCanceledException)
        {
            authenticationWindow?.Complete();
            ShowAuthenticationError(
                desktop,
                "Время ожидания входа истекло. Запустите RevEx и попробуйте снова.");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"OIDC startup authentication failed: {exception}");
            authenticationWindow?.Complete();
            ShowAuthenticationError(desktop, exception.Message);
        }
    }

    private static void ShowAuthenticationError(
        IClassicDesktopStyleApplicationLifetime desktop,
        string message)
    {
        var errorWindow = new AuthenticationErrorWindow(message);
        desktop.MainWindow = errorWindow;
        errorWindow.Closed += (_, _) => desktop.Shutdown(1);
        errorWindow.Show();
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


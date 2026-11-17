using System;
using System.Threading.Tasks;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
    private readonly CancellationTokenSource _startupCancellation = new();
    private Task? _shutdownTask;
    private Task? _startupTask;
    private bool _connectorServicesStarted;

    public static IServiceProvider Services { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DesktopThemePreference.Apply(this, DesktopThemePreference.Load());
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var configuration = CreateConfig();
        var services = new ServiceCollection();

        services.AddServices(configuration);

        services.AddSingleton<IApplicationShutdownService>(
            new ApplicationShutdownService(() => RequestShutdownAsync(signOut: true)));
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.ShutdownRequested += (_, e) =>
            {
                e.Cancel = true;
                _ = RequestShutdownAsync();
            };
            desktop.Startup += (_, _) => _startupTask = StartDesktopAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private Task RequestShutdownAsync(bool signOut = false, int exitCode = 0)
    {
        return _shutdownTask ??= ShutdownAsync(signOut, exitCode);
    }

    private async Task ShutdownAsync(bool signOut, int exitCode)
    {
        // Let Avalonia finish delivering the event and publish the shared shutdown task.
        await Dispatcher.Yield(DispatcherPriority.Background);

        var desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
        try
        {
            if (desktop.MainWindow is { } window)
            {
                window.IsEnabled = false;
            }

            _startupCancellation.Cancel();
            if (_startupTask is { } startup)
            {
                await RunShutdownStepAsync("завершение запуска", token => startup.WaitAsync(token));
            }

            if (signOut)
            {
                await RunShutdownStepAsync("выход из учётной записи",
                    token => Services.GetRequiredService<IAuthenticationService>().LogoutAsync(token));
            }

            if (_connectorServicesStarted)
            {
                await RunShutdownStepAsync("остановка Desktop Control Bridge",
                    token => Services.GetRequiredService<DesktopControlBridge>().StopAsync(token));
                await RunShutdownStepAsync("остановка коннекторов",
                    token => Services.GetRequiredService<IConnectorRuntime>().StopAsync(token));
                await RunShutdownStepAsync("освобождение клиента коннектора", _ =>
                {
                    (Services.GetRequiredService<RevEx.Connector.Contracts.IConnectorClient>() as IDisposable)?.Dispose();
                    return Task.CompletedTask;
                });
            }
        }
        finally
        {
            desktop.Shutdown(exitCode);
        }
    }

    private static async Task RunShutdownStepAsync(string operation, Func<CancellationToken, Task> action)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await action(timeout.Token).WaitAsync(timeout.Token);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Ошибка при завершении Desktop ({operation}): {error}");
        }
    }

    private async Task StartDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        AuthenticationProgressWindow? authenticationWindow = null;
        try
        {
            if (!await Services.GetRequiredService<StartupUpdateCoordinator>().RunAsync())
            {
                _ = RequestShutdownAsync();
                return;
            }
            _startupCancellation.Token.ThrowIfCancellationRequested();
            authenticationWindow = new AuthenticationProgressWindow(
                Services.GetRequiredService<IAuthenticationBrowser>());
            desktop.MainWindow = authenticationWindow;
            authenticationWindow.Show();

            using var authenticationTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            using var authenticationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                authenticationTimeout.Token,
                authenticationWindow.CancellationToken, _startupCancellation.Token);
            var authenticationService = Services.GetRequiredService<IAuthenticationService>();
            var accessToken = await authenticationService.GetAccessTokenAsync(
                authenticationCancellation.Token);
            authenticationWindow.Complete();
            authenticationWindow = null;

            _startupCancellation.Token.ThrowIfCancellationRequested();
            _connectorServicesStarted = true;
            await Services.GetRequiredService<IConnectorRuntime>().StartAsync(_startupCancellation.Token);
            _startupCancellation.Token.ThrowIfCancellationRequested();
            await Services.GetRequiredService<DesktopControlBridge>().StartAsync(_startupCancellation.Token);
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_startupCancellation.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                await Services.GetRequiredService<DesktopUpdateService>().ReportAuthenticatedAsync(
                    Services.GetRequiredService<IAppSettings>().ApiPath, accessToken, timeout.Token);
            }
            catch (Exception trackingError)
            {
                // Retry on the next startup. Telemetry failure must not prevent authenticated work.
                Console.Error.WriteLine($"Не удалось подтвердить версию десктопа: {trackingError.Message}");
            }

            _startupCancellation.Token.ThrowIfCancellationRequested();
            var mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            mainWindow.Closing += (_, e) =>
            {
                e.Cancel = true;
                _ = RequestShutdownAsync();
            };
            mainWindow.Show();
        }
        catch (OperationCanceledException) when (
            _startupCancellation.IsCancellationRequested ||
            authenticationWindow?.CancellationToken.IsCancellationRequested == true)
        {
            _ = RequestShutdownAsync();
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

    private void ShowAuthenticationError(
        IClassicDesktopStyleApplicationLifetime desktop,
        string message)
    {
        var errorWindow = new AuthenticationErrorWindow(message);
        desktop.MainWindow = errorWindow;
        errorWindow.Closed += (_, _) => _ = RequestShutdownAsync(exitCode: 1);
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


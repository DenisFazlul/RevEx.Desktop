using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.ViewModels.MainMenu;
using RevEx.Desktop.Views;
using RevEx.Desktop.Views.MainMenu;

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
        var services = new ServiceCollection();

        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5023/")
        });
        //services.AddTransient<IRevExApiService, RevExApiService>();
        services.AddTransient<IRevExApiService, MockRevExApiService>();
        services.AddSingleton<IMainMenuProvider, MainMenuProvider>();
        services.AddTransient<IWorkspaceTabFactory, WorkspaceTabFactory>();

        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<MainMenuView>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

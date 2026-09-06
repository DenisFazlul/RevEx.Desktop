using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.DI;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.ViewModels;
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
        var configuration = CreateConfig();
        var services = new ServiceCollection();
        
        services.AddServices(configuration);

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private IConfigurationRoot CreateConfig()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        return configuration;
    }
}

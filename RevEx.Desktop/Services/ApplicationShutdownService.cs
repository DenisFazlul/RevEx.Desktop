using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace RevEx.Desktop.Services;

public sealed class ApplicationShutdownService : IApplicationShutdownService
{
    public void Shutdown()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}

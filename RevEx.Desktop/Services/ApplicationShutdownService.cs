using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace RevEx.Desktop.Services;

public sealed class ApplicationShutdownService : IApplicationShutdownService
{
    public void Shutdown()
    {
        Dispatcher.UIThread.Post(
            () =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.Shutdown();
            },
            DispatcherPriority.Background);
    }
}

using Avalonia;
using System;
using System.IO;
using Velopack;

namespace RevEx.Desktop;

sealed class Program
{
    private const string SingleInstanceLockFileName = "RevEx.Desktop.instance.lock";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Even an already downloaded update requires the user's startup decision.
        VelopackApp.Build().SetAutoApplyOnStartup(false).Run();

        using var instanceLock = TryAcquireInstanceLock();
        if (instanceLock is null)
        {
            Console.Error.WriteLine("RevEx Desktop is already running.");
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static FileStream? TryAcquireInstanceLock()
    {
        var lockFilePath = Path.Combine(Path.GetTempPath(), SingleInstanceLockFileName);

        try
        {
            return new FileStream(
                lockFilePath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

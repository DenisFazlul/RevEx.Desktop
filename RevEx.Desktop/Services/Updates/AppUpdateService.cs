using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace RevEx.Desktop.Services.Updates;

public interface IAppUpdateService
{
    string CurrentVersion { get; }
    Task<UpdateInfo?> CheckAsync();
    Task DownloadAsync(UpdateInfo update, Action<int> progress, CancellationToken cancellationToken);
    void ApplyAndRestart(UpdateInfo update);
}

public sealed class AppUpdateService(UpdateSettings settings) : IAppUpdateService
{
    private UpdateManager? _manager;
    private UpdateManager Manager => _manager ??= new UpdateManager(
        new GithubSource(settings.RepositoryUrl, null, false));

    public string CurrentVersion => Manager.CurrentVersion?.ToString() ?? "—";

    public async Task<UpdateInfo?> CheckAsync()
    {
        if (!settings.Enabled || !Manager.IsInstalled)
            return null;

        return await Manager.CheckForUpdatesAsync().WaitAsync(
            TimeSpan.FromSeconds(Math.Clamp(settings.CheckTimeoutSeconds, 1, 30)));
    }

    public Task DownloadAsync(UpdateInfo update, Action<int> progress,
        CancellationToken cancellationToken) =>
        Manager.DownloadUpdatesAsync(update, progress, cancellationToken);

    public void ApplyAndRestart(UpdateInfo update) =>
        Manager.ApplyUpdatesAndRestart(update.TargetFullRelease);
}

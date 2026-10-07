using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using RevEx.Desktop.Core.Domain;
using Velopack;

namespace RevEx.Desktop.Services.Updates;

public sealed record DesktopUpdateCheck(DesktopReleaseDto Release, bool Compatible, UpdateManager Manager);

public sealed class DesktopUpdateService(HttpClient httpClient)
{
    public bool IsInstalled => new UpdateManager("https://localhost/").IsInstalled;
    public string CurrentVersion => new UpdateManager("https://localhost/").CurrentVersion?.ToString()
        ?? DesktopClientVersion.Value;

    public async Task<DesktopUpdateCheck> CheckAsync(string address, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(address.TrimEnd('/') + "/", UriKind.Absolute, out var backend)
            || backend.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Укажите корректный HTTP(S)-адрес backend.");
        var release = await httpClient.GetFromJsonAsync<DesktopReleaseDto>(new Uri(backend, "api/desktop-release"), cancellationToken)
            ?? throw new InvalidOperationException("Backend не вернул сведения о релизе.");
        var current = SemanticVersion.Parse(CurrentVersion);
        var minimum = SemanticVersion.Parse(release.MinimumDesktopVersion);
        var maximum = SemanticVersion.Parse(release.MaximumDesktopVersion);
        var recommended = SemanticVersion.Parse(release.RecommendedDesktopVersion);
        if (minimum > recommended || recommended > maximum)
            throw new InvalidOperationException("Backend вернул некорректный диапазон совместимости.");
        var source = new Uri(backend, release.UpdatesPath);
        if (source.Scheme != backend.Scheme || source.Authority != backend.Authority || !source.AbsolutePath.StartsWith(backend.AbsolutePath, StringComparison.Ordinal))
            throw new InvalidOperationException("Источник обновления должен находиться на выбранном backend.");
        var manager = new UpdateManager(source.ToString(), new UpdateOptions
        {
            AllowVersionDowngrade = true,
            MaximumDeltasBeforeFallback = -1
        });
        return new DesktopUpdateCheck(release, current >= minimum && current <= maximum, manager);
    }

    public async Task InstallAsync(DesktopUpdateCheck check, Action<int> progress, CancellationToken cancellationToken)
    {
        var update = await check.Manager.CheckForUpdatesAsync().WaitAsync(TimeSpan.FromSeconds(30), cancellationToken)
            ?? throw new InvalidOperationException("Пакет рекомендованной версии не найден. Повторите проверку.");
        if (update.TargetFullRelease.Version.ToString() != check.Release.RecommendedDesktopVersion)
            throw new InvalidOperationException("Версия пакета отличается от рекомендованной backend.");
        await check.Manager.DownloadUpdatesAsync(update, progress, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        check.Manager.ApplyUpdatesAndRestart(update.TargetFullRelease);
    }
}

public static class DesktopClientVersion
{
    public static string Value => typeof(DesktopClientVersion).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
}

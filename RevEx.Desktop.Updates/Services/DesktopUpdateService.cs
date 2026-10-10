using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using Velopack;

namespace RevEx.Desktop.Updates.Services;

public sealed record DesktopUpdateCheck(DesktopUpdateResponse Response, Uri Backend)
{
    public bool Compatible => Response.Decision is "current" or "optional";
    public bool HasUpdate => Response.Decision is "optional" or "required";
}

public sealed class DesktopUpdateService(HttpClient httpClient, DesktopInstallationState installation)
{
    public bool IsInstalled => DesktopUpdates.InstalledVersion is not null;
    public string CurrentVersion => DesktopUpdates.InstalledVersion ?? DesktopClientVersion.Value;
    private static string Platform => (OperatingSystem.IsWindows(), OperatingSystem.IsMacOS(), RuntimeInformation.ProcessArchitecture) switch
    {
        (true, _, Architecture.X64) => "win-x64",
        (_, true, Architecture.X64) => "osx-x64",
        (_, true, Architecture.Arm64) => "osx-arm64",
        _ => throw new InvalidOperationException("Эта ОС или архитектура не поддерживается обновлениями RevEx.")
    };

    public async Task<DesktopUpdateCheck> CheckAsync(string address, CancellationToken cancellationToken)
    {
        var backend = Backend(address);
        var pending = installation.Pending(backend.ToString());
        if (pending != null && pending.TargetVersion != CurrentVersion)
        {
            await ReportFailureAsync(backend, pending.OfferId, "После перезапуска назначенная версия не установлена.", cancellationToken);
            installation.SetPending(backend.ToString(), null);
        }
        using var response = await httpClient.PostAsJsonAsync(new Uri(backend, "api/desktop-updates/check"), Request(backend), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var decision = await response.Content.ReadFromJsonAsync<DesktopUpdateResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Backend не вернул решение об обновлении.");
        if (decision.Decision is not ("current" or "optional" or "required"))
            throw new InvalidOperationException("Backend вернул неизвестное решение.");
        if (decision.Decision != "current")
        {
            if (decision.Package is not { } package || decision.OfferId == null || decision.DownloadPath == null
                || package.Version != decision.TargetVersion || package.Platform != Platform || package.Size <= 0
                || !System.Text.RegularExpressions.Regex.IsMatch(package.Sha256 ?? "", "^[A-Fa-f0-9]{64}$")
                || Path.GetFileName(package.FileName) != package.FileName || package.FileName.Contains('\\')
                || !package.FileName.EndsWith(".nupkg", StringComparison.Ordinal))
                throw new InvalidOperationException("Backend не назначил корректный пакет обновления.");
            DownloadUri(backend, decision.DownloadPath);
        }
        return new(decision, backend);
    }

    public async Task InstallAsync(DesktopUpdateCheck check, Action<int> progress, CancellationToken cancellationToken)
    {
        var response = check.Response;
        var package = response.Package ?? throw new InvalidOperationException("Backend не назначил пакет.");
        var asset = new VelopackAsset
        {
            PackageId = "RevEx.Desktop", Version = SemanticVersion.Parse(package.Version), Type = VelopackAssetType.Full,
            FileName = package.FileName, Size = package.Size, SHA256 = package.Sha256, SHA1 = package.Sha1
        };
        var manager = new UpdateManager(new OfferedUpdateSource(httpClient, DownloadUri(check.Backend, response.DownloadPath!), installation.Key, package),
            new UpdateOptions { AllowVersionDowngrade = true, MaximumDeltasBeforeFallback = -1 });
        try
        {
            await manager.DownloadUpdatesAsync(new UpdateInfo(asset, response.IsDowngrade), progress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            installation.SetPending(check.Backend.ToString(), new(response.OfferId!.Value, package.Version));
            manager.ApplyUpdatesAndRestart(asset);
        }
        catch (Exception exception)
        {
            installation.SetPending(check.Backend.ToString(), null);
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await ReportFailureAsync(check.Backend, response.OfferId!.Value,
                    exception is OperationCanceledException ? "Скачивание отменено." : exception.Message, timeout.Token);
            }
            catch (Exception reportError) { Console.Error.WriteLine($"Не удалось сообщить об ошибке обновления: {reportError.Message}"); }
            throw;
        }
    }

    public async Task ReportAuthenticatedAsync(string address, string accessToken, CancellationToken cancellationToken)
    {
        if (!IsInstalled) return;
        var backend = Backend(address);
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(backend, "api/desktop-updates/report"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Content = JsonContent.Create(Request(backend));
        using var response = await httpClient.SendAsync(message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        installation.SetPending(backend.ToString(), null);
    }

    private DesktopInstallationRequest Request(Uri backend)
    {
        var pending = installation.Pending(backend.ToString());
        return new(installation.Id, installation.Key, CurrentVersion, Platform,
            pending?.TargetVersion == CurrentVersion ? pending.OfferId : null);
    }
    private async Task ReportFailureAsync(Uri backend, Guid offerId, string message, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(backend, $"api/desktop-updates/offers/{offerId}/failure"));
        request.Headers.Add("X-RevEx-Installation-Key", installation.Key);
        request.Content = JsonContent.Create(new { message = message[..Math.Min(message.Length, 2000)] });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }
    private static Uri Backend(string address)
    {
        if (!Uri.TryCreate(address.TrimEnd('/') + "/", UriKind.Absolute, out var backend)
            || backend.Scheme is not ("http" or "https") || backend.UserInfo.Length != 0 || backend.Query.Length != 0 || backend.Fragment.Length != 0)
            throw new InvalidOperationException("Укажите корректный HTTP(S)-адрес backend.");
        return backend;
    }
    private static Uri DownloadUri(Uri backend, string path)
    {
        var source = new Uri(backend, path);
        if (source.Scheme != backend.Scheme || source.Authority != backend.Authority || source.UserInfo.Length != 0
            || !source.AbsolutePath.StartsWith(backend.AbsolutePath, StringComparison.Ordinal))
            throw new InvalidOperationException("Источник обновления должен находиться на выбранном backend.");
        return source;
    }
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("message", out var message)) throw new InvalidOperationException(message.GetString());
        }
        catch (System.Text.Json.JsonException) { }
        response.EnsureSuccessStatusCode();
    }
}

public static class DesktopClientVersion
{
    public static string Value => (System.Reflection.Assembly.GetEntryAssembly() ?? typeof(DesktopClientVersion).Assembly).GetName().Version?.ToString(3) ?? "1.0.0";
}

using System.Net.Http;
using System.Security.Cryptography;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace RevEx.Desktop.Updates.Services;

// The backend has already selected a full package. No client-side feed selection occurs.
internal sealed class OfferedUpdateSource(HttpClient client, Uri download, string key, DesktopPackage package) : IUpdateSource
{
    public Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel,
        Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
        => throw new NotSupportedException("Пакет назначается backend при проверке.");

    public async Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile,
        Action<int> progress, CancellationToken cancelToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, download);
        request.Headers.Add("X-RevEx-Installation-Key", key);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancelToken);
        await using var output = File.Create(localFile);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long received = 0;
        int count;
        while ((count = await input.ReadAsync(buffer, cancelToken)) > 0)
        {
            received += count;
            if (received > package.Size) throw new InvalidOperationException("Размер пакета превышает назначенный backend.");
            hash.AppendData(buffer, 0, count);
            await output.WriteAsync(buffer.AsMemory(0, count), cancelToken);
            progress((int)(received * 100 / package.Size));
        }
        if (received != package.Size || !Convert.ToHexString(hash.GetHashAndReset()).Equals(package.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Пакет не прошёл проверку целостности.");
    }
}

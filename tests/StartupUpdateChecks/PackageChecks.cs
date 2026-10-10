using System.Net;
using System.Security.Cryptography;
using RevEx.Desktop.Updates;
using RevEx.Desktop.Updates.Services;
using Velopack;

internal static class PackageChecks
{
    public static async Task RunAsync(string root)
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        var package = new DesktopPackage("2.0.0", "win-x64", "RevEx-full.nupkg", bytes.Length,
            Convert.ToHexString(SHA256.HashData(bytes)), Convert.ToHexString(SHA1.HashData(bytes)));
        var path = Path.Combine(root, "package.nupkg");
        var asset = new VelopackAsset();
        var key = new string('A', 64);
        var source = new OfferedUpdateSource(new HttpClient(new Handler(bytes, key)), new Uri("https://backend.example/package"), key, package);
        await source.DownloadReleaseEntry(null!, asset, path, _ => { });
        if (!File.ReadAllBytes(path).SequenceEqual(bytes)) throw new Exception("Downloaded package differs");
        Console.WriteLine("PASS: Assigned package downloaded with installation key and verified hash");
        var corrupt = new OfferedUpdateSource(new HttpClient(new Handler([5, 4, 3, 2, 1], key)), new Uri("https://backend.example/package"), key, package);
        try
        {
            await corrupt.DownloadReleaseEntry(null!, asset, path, _ => { });
            throw new Exception("Corrupt package was accepted");
        }
        catch (InvalidOperationException) { Console.WriteLine("PASS: Corrupt package rejected before installation"); }
        var oversized = new OfferedUpdateSource(new HttpClient(new Handler([1, 2, 3, 4, 5, 6], key)), new Uri("https://backend.example/package"), key, package);
        try
        {
            await oversized.DownloadReleaseEntry(null!, asset, path, _ => { });
            throw new Exception("Oversized package was accepted");
        }
        catch (InvalidOperationException) { Console.WriteLine("PASS: Package size limit enforced"); }
    }
    private sealed class Handler(byte[] bytes, string key) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.GetValues("X-RevEx-Installation-Key").Single() != key)
                throw new Exception("Installation identity missing from package request");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        }
    }
}

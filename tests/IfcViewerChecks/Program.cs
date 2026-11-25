using System.Net;
using System.Text;
using RevEx.Desktop.IfcViewer;

var ifc = Encoding.UTF8.GetBytes(
    "ISO-10303-21;\nHEADER;\nFILE_SCHEMA(('IFC4'));\nENDSEC;\nDATA;\nENDSEC;\nEND-ISO-10303-21;");

Require(IfcViewerFile.IsSupported("sample.ifc"), "Lowercase IFC extension was not recognized.");
Require(IfcViewerFile.IsSupported("SAMPLE.IFC"), "Uppercase IFC extension was not recognized.");
Require(!IfcViewerFile.IsSupported("sample.rvt"), "A non-IFC extension was accepted.");

await using var server = await IfcViewerServer.StartAsync("sample.ifc", ifc, CancellationToken.None);
using var client = new HttpClient();

var indexResponse = await client.GetAsync(server.ViewerUri);
Require(
    indexResponse.StatusCode == HttpStatusCode.OK,
    $"Viewer index was not served: {(int)indexResponse.StatusCode} {indexResponse.StatusCode}.");
Require(indexResponse.Headers.CacheControl?.NoStore == true, "Viewer responses must disable caching.");
Require(
    indexResponse.Headers.TryGetValues("Content-Security-Policy", out var policies) && policies.Any(),
    "Viewer response has no content security policy.");

await RequireAssetAsync("viewer.js", "text/javascript");
await RequireAssetAsync("viewer.css", "text/css");
await RequireAssetAsync("worker.mjs", "text/javascript");
await RequireAssetAsync("wasm/web-ifc.wasm", "application/wasm");
await RequireAssetAsync("wasm/web-ifc-mt.wasm", "application/wasm");

var model = await client.GetByteArrayAsync(new Uri(server.ViewerUri, "model.ifc"));
Require(model.SequenceEqual(ifc), "Viewer returned different IFC bytes.");

Console.WriteLine("PASS: isolated IFC viewer serves bundled UI, worker, WASM and the selected model on loopback.");
return;

async Task RequireAssetAsync(string path, string mediaType)
{
    var response = await client.GetAsync(new Uri(server.ViewerUri, path));
    Require(response.StatusCode == HttpStatusCode.OK, $"Viewer asset {path} was not served.");
    Require(response.Content.Headers.ContentType?.MediaType == mediaType, $"Viewer asset {path} has a wrong media type.");
    var content = await response.Content.ReadAsByteArrayAsync();
    Require(content.Length > 0, $"Viewer asset {path} is empty.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

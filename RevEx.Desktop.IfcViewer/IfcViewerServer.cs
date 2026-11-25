using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RevEx.Desktop.IfcViewer;

/// <summary>Раздаёт ресурсы просмотрщика и IFC только на случайном loopback-адресе.</summary>
internal sealed class IfcViewerServer : IAsyncDisposable
{
    private const string ResourcePrefix = "RevEx.Desktop.IfcViewer.Web.dist";
    private readonly WebApplication _application;

    private IfcViewerServer(WebApplication application, Uri viewerUri)
    {
        _application = application;
        ViewerUri = viewerUri;
    }

    public Uri ViewerUri
    {
        get;
    }

    public static async Task<IfcViewerServer> StartAsync(
        string fileName,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString("N");
        var routePrefix = $"/{token}";
        var index = ReadResource("index.html");
        var script = ReadResource("viewer.js");
        var style = ReadResource("viewer.css");
        var worker = ReadResource("worker.mjs");
        var wasm = ReadResource("wasm.web-ifc.wasm");
        var multithreadedWasm = ReadResource("wasm.web-ifc-mt.wasm");

        var options = new WebApplicationOptions
        {
            ApplicationName = typeof(IfcViewerServer).Assembly.GetName().Name,
            Args = []
        };
        var builder = WebApplication.CreateSlimBuilder(options);
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0));

        var application = builder.Build();
        application.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
            context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
            context.Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; " +
                "connect-src 'self'; worker-src 'self' blob:; style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob:;");
            await next(context);
        });

        application.MapGet(routePrefix, () => Results.Bytes(index, "text/html; charset=utf-8"));
        application.MapGet($"{routePrefix}/viewer.js", () => Results.Bytes(script, "text/javascript; charset=utf-8"));
        application.MapGet($"{routePrefix}/viewer.css", () => Results.Bytes(style, "text/css; charset=utf-8"));
        application.MapGet($"{routePrefix}/worker.mjs", () => Results.Bytes(worker, "text/javascript; charset=utf-8"));
        application.MapGet($"{routePrefix}/wasm/web-ifc.wasm", () => Results.Bytes(wasm, "application/wasm"));
        application.MapGet(
            $"{routePrefix}/wasm/web-ifc-mt.wasm",
            () => Results.Bytes(multithreadedWasm, "application/wasm"));
        application.MapGet(
            $"{routePrefix}/model.ifc",
            () => Results.File(content, "application/octet-stream", fileName, enableRangeProcessing: false));

        await application.StartAsync(cancellationToken);

        var server = application.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var address = addresses?.SingleOrDefault();
        if (string.IsNullOrWhiteSpace(address))
        {
            await application.DisposeAsync();
            throw new InvalidOperationException("Не удалось определить локальный адрес IFC-просмотрщика.");
        }

        var viewerUri = new Uri($"{address.TrimEnd('/')}{routePrefix}/");
        return new IfcViewerServer(application, viewerUri);
    }

    public async ValueTask DisposeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await _application.StopAsync(timeout.Token);
        await _application.DisposeAsync();
    }

    private static byte[] ReadResource(string name)
    {
        var assembly = typeof(IfcViewerServer).Assembly;
        var resourceName = $"{ResourcePrefix}.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"Ресурс просмотрщика {resourceName} не найден. Доступны: {availableResources}");
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

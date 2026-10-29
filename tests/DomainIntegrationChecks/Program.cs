using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using RevEx.Connector.Host;
using RevEx.Desktop.Connectors;
using RevEx.Domain.Contracts.Connectors;
using RevEx.LoadingQueue;

static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
var probe = new TcpListener(IPAddress.Loopback,0); probe.Start();
var port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
var address = new Uri($"http://127.0.0.1:{port}/");
var registry = new ConnectorRegistry(); var queue = new FileLoadingQueue();
await using var server = new DesktopConnectorServer(registry,queue,address);
await server.StartAsync();
using var http = new HttpClient(new HttpClientHandler { UseProxy=false }) { Timeout=TimeSpan.FromSeconds(10) };
var client = new ConnectorClient(http);
var fileId = Guid.NewGuid(); queue.Enqueue(fileId,"Family.rfa");
var start = new ProcessStartInfo(Path.GetFullPath(args[0])) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true };
start.ArgumentList.Add(address.ToString()); start.ArgumentList.Add(fileId.ToString());
using var legacy = Process.Start(start)!;
var errors = legacy.StandardError.ReadToEndAsync();
var output = legacy.StandardOutput.ReadToEndAsync();
try
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
    while(registry.GetAll().Count==0) { if(legacy.HasExited) throw new Exception(await errors); await Task.Delay(100,timeout.Token); }
    var connector = registry.GetAll().Single();
    var health = await client.GetHealthAsync(connector);
    Require(health.InstanceId==connector.InstanceId && health.Status=="ready","Framework health response incompatible");
    using var missing = await http.GetAsync(new Uri(connector.BaseAddress,"missing"));
    Require(missing.StatusCode==HttpStatusCode.NotFound,"Expected 404");
    Require((await missing.Content.ReadFromJsonAsync<RevitApiErrorResponse>())?.Error.Code=="NOT_FOUND","Framework error JSON incompatible");
    using var load = await http.PostAsJsonAsync(new Uri(connector.BaseAddress,"api/v1/projects/active/content-versions/load"),new LoadContentVersionRequest(new[]{new ContentVersionLoadFile(fileId,"Family.rfa","Family.rfa")},address));
    Require(load.StatusCode==HttpStatusCode.NotImplemented,"Existing Revit loading behavior changed");
    while(queue.GetAll().Single().Status!=FileLoadingStatus.Completed) await Task.Delay(100,timeout.Token);
    Console.WriteLine("PASS: .NET Framework registration, health, error responses and status callback with modern Desktop.");
}
finally
{
    if(!legacy.HasExited) { await legacy.StandardInput.WriteLineAsync(); if(!legacy.WaitForExit(5000)) legacy.Kill(); }
    await output; await errors;
}
await using var modern = new ConnectorHost(new ConnectorHostOptions("Contract test",ConnectorProduct.Mock,"1.0",address));
modern.ContentVersionLoadRequested += (request,token) => Task.FromResult(new LoadContentVersionResponse(true,request.Files.Count));
await modern.StartAsync();
var modernDescriptor=registry.GetAll().Single(c=>c.Product==ConnectorProduct.Mock);
var result=await client.LoadContentVersionAsync(modernDescriptor,new[]{new ContentVersionLoadFile(fileId,"Family.rfa","Family.rfa")},address);
Require(result.Success && result.LoadedFileCount==1,"Modern connector load contract incompatible");
using var invalid=await http.PostAsJsonAsync(new Uri(address,"api/v1/connectors/register"),new ConnectorRegistrationRequest(Guid.NewGuid(),"bad",ConnectorProduct.Mock,"1","https://example.com",1,null));
Require(invalid.StatusCode==HttpStatusCode.BadRequest,"Invalid registration accepted");
Console.WriteLine("PASS: modern Connector host registration, load request and invalid address rejection.");

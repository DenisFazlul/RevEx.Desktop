using System.Net;
using System.Net.Http.Json;
using RevEx.Configuration;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.Services.Updates;
using RevEx.Desktop.ViewModels.Updates;

Velopack.VelopackApp.Build().SetAutoApplyOnStartup(false).Run();

void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS: " + name);
}
var settings = new Settings();
var saved = new SavedSettings();
StartupUpdateViewModel Model(string minimum, string maximum, string recommended, string path = "api/desktop-releases/1.0.1/files/", bool fail = false)
    => new(new DesktopUpdateService(new HttpClient(new Handler(minimum, maximum, recommended, path, fail))), settings, saved);

var current = new DesktopUpdateService(new HttpClient()).CurrentVersion;
var same = Model(current, current, current);
await same.CheckCommand.ExecuteAsync(null);
Check(same.Decision.IsCompletedSuccessfully && await same.Decision, "Matching desktop continues startup automatically");
var optional = Model("0.0.0", "999.0.0", "999.0.0");
await optional.CheckCommand.ExecuteAsync(null);
Check(optional.CanContinue && optional.HasUpdate && !optional.Decision.IsCompleted, "Compatible desktop waits for user consent");
optional.ContinueWithoutUpdateCommand.Execute(null);
Check(await optional.Decision, "User may decline optional update");
Check(saved.Value.ConnectorRegistrationAddress == "http://127.0.0.1:7891/", "Saving backend keeps connector address");
var required = Model("999.0.0", "999.0.0", "999.0.0");
await required.CheckCommand.ExecuteAsync(null);
required.ContinueWithoutUpdateCommand.Execute(null);
Check(!required.CanContinue && required.HasUpdate && !required.Decision.IsCompleted, "Incompatible desktop cannot continue");
required.RequestClose();
Check(!await required.Decision, "Required update may be declined by exiting");
var unavailable = Model(current, current, current, fail: true);
await unavailable.CheckCommand.ExecuteAsync(null);
Check(unavailable.Error != null && !unavailable.CanContinue && !unavailable.HasUpdate, "Unavailable backend never silently bypasses check");
var external = Model(current, current, current, path: "https://example.com/releases/");
await external.CheckCommand.ExecuteAsync(null);
Check(external.Error != null && !external.CanContinue, "Foreign update source rejected");
var broken = Model("999.0.0", "999.0.0", "1.0.0");
await broken.CheckCommand.ExecuteAsync(null);
Check(broken.Error != null, "Invalid backend compatibility range rejected");

sealed class Handler(string minimum, string maximum, string recommended, string path, bool fail) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(fail ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
        { Content = JsonContent.Create(new { backendVersion = "1.0.0", recommendedDesktopVersion = recommended,
            minimumDesktopVersion = minimum, maximumDesktopVersion = maximum, updatesPath = path,
            installers = new Dictionary<string, string>(), notes = "Test" }) });
}
sealed class Settings : IAppSettings
{
    public string ApiPath { get; set; } = "http://localhost:8080/";
    public string ConnectorRegistrationAddress { get; set; } = "http://127.0.0.1:7891/";
    public AuthenticationSettings Authentication { get; set; } = new();
}
sealed class SavedSettings : IRevExUserSettingsStore
{
    public string FilePath => "memory";
    public RevExUserSettings Value { get; private set; } = new() { ConnectorRegistrationAddress = "http://127.0.0.1:7891/" };
    public RevExUserSettings Load() => Value;
    public void Save(RevExUserSettings settings) => Value = settings;
}

using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;
using RevEx.Configuration;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.Updates;
using RevEx.Desktop.Updates.Services;
using RevEx.Desktop.Updates.ViewModels;

DesktopUpdates.Initialize();
var root = Path.Combine(Path.GetTempPath(), "revex-update-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }
    await PackageChecks.RunAsync(root);
    var settings = new Settings();
    var saved = new SavedSettings();
    var state = new DesktopInstallationState(Path.Combine(root, "installation.json"));
    var reloaded = new DesktopInstallationState(Path.Combine(root, "installation.json"));
    Check(state.Id == reloaded.Id && state.Key == reloaded.Key, "Installation identity survives restart");
    var offerId = Guid.NewGuid();
    state.SetPending("http://localhost:8080/", new(offerId, "1.0.0"));
    Check(new DesktopInstallationState(Path.Combine(root, "installation.json")).Pending("http://localhost:8080/")?.OfferId == offerId,
        "Pending update survives restart");
    state.SetPending("http://localhost:8080/", null);
    StartupUpdateViewModel Model(string decision, string target = "99.0.0", string? path = null, bool fail = false)
        => new(new DesktopUpdateService(new HttpClient(new Handler(decision, target, path, fail)), state), settings, saved);

    var same = Model("current", "1.0.0");
    await same.CheckCommand.ExecuteAsync(null);
    Check(same.Decision.IsCompletedSuccessfully && await same.Decision, "Backend current decision continues startup automatically");
    var optional = Model("optional");
    await optional.CheckCommand.ExecuteAsync(null);
    Check(optional.CanContinue && optional.HasUpdate && !optional.Decision.IsCompleted, "Backend optional decision waits for user");
    optional.ContinueWithoutUpdateCommand.Execute(null);
    Check(await optional.Decision, "Optional update may be declined");
    Check(saved.Value.ConnectorRegistrationAddress == "http://127.0.0.1:7891/", "Saving backend preserves connector address");
    var required = Model("required");
    await required.CheckCommand.ExecuteAsync(null);
    required.ContinueWithoutUpdateCommand.Execute(null);
    Check(!required.CanContinue && required.HasUpdate && !required.Decision.IsCompleted, "Backend required decision blocks startup");
    required.RequestClose();
    Check(!await required.Decision, "Required update may be declined by exiting");
    var rollback = Model("required", "0.9.0");
    await rollback.CheckCommand.ExecuteAsync(null);
    Check(rollback.HasUpdate && !rollback.CanContinue && rollback.RecommendedVersion == "0.9.0", "Backend-selected downgrade is offered without client selection");
    var unavailable = Model("current", fail: true);
    await unavailable.CheckCommand.ExecuteAsync(null);
    Check(unavailable.Error != null && !unavailable.CanContinue && !unavailable.HasUpdate, "Unavailable backend cannot bypass check");
    var external = Model("required", path: "https://example.com/package");
    await external.CheckCommand.ExecuteAsync(null);
    Check(external.Error != null && !external.CanContinue, "Foreign download source rejected");
    var broken = Model("unknown");
    await broken.CheckCommand.ExecuteAsync(null);
    Check(broken.Error != null, "Unknown backend decision rejected");
}
finally { Directory.Delete(root, true); }

sealed class Handler(string decision, string target, string? path, bool fail) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri!.AbsolutePath != "/api/desktop-updates/check" || request.Method != HttpMethod.Post)
            throw new Exception("Client must use a single backend decision request");
        var input = await request.Content!.ReadFromJsonAsync<DesktopInstallationRequest>(cancellationToken);
        var package = decision == "current" ? null : new DesktopPackage(target, input!.Platform,
            $"RevEx-{target}-{input.Platform}-full.nupkg", 100, new string('A', 64), new string('B', 40));
        return new HttpResponseMessage(fail ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
        { Content = JsonContent.Create(new DesktopUpdateResponse(decision, target, "Test", package, target == "0.9.0",
            package == null ? null : Guid.NewGuid(), package == null ? null : path ?? "api/desktop-updates/offers/00000000-0000-0000-0000-000000000001/package")) };
    }
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

using System.Security.Cryptography;
using System.Text.Json;

namespace RevEx.Desktop.Updates.Services;

public sealed record PendingDesktopUpdate(Guid OfferId, string TargetVersion);

// Kept outside the application directory so updates and downgrades preserve the identity.
public sealed class DesktopInstallationState
{
    private readonly string _path;
    private readonly State _state;
    public Guid Id => _state.InstallationId;
    public string Key => _state.InstallationKey;

    public DesktopInstallationState(string path)
    {
        _path = path;
        _state = File.Exists(path) ? JsonSerializer.Deserialize<State>(File.ReadAllText(path))
            ?? throw new InvalidOperationException("Повреждены сведения об установке.") : new State();
        Save();
    }

    public PendingDesktopUpdate? Pending(string backend) => _state.Pending.GetValueOrDefault(backend);
    public void SetPending(string backend, PendingDesktopUpdate? update)
    {
        if (update == null) _state.Pending.Remove(backend);
        else _state.Pending[backend] = update;
        Save();
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        var temporary = _path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(_state));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temporary, _path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public sealed class State
    {
        public Guid InstallationId { get; set; } = Guid.NewGuid();
        public string InstallationKey { get; set; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        public Dictionary<string, PendingDesktopUpdate> Pending { get; set; } = new();
    }
}

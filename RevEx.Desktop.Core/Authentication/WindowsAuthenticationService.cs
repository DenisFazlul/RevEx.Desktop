using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Authentication;

public sealed class WindowsAuthenticationService : IAuthenticationService
{
    public IReadOnlySet<string> Roles { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public string? CurrentAuthor
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return identity.Name;
        }
    }

    public bool IsInRole(string role) => Roles.Contains(role);

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Empty);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

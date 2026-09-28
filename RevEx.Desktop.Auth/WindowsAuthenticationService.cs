using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Auth;

public sealed class WindowsAuthenticationService : IAuthenticationService
{
    public IReadOnlySet<string> Roles { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool IsInRole(string role) => Roles.Contains(role);

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Empty);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

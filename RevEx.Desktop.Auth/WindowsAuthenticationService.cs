using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Auth;

public sealed class WindowsAuthenticationService : IAuthenticationService
{
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Empty);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

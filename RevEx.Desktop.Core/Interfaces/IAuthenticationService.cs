namespace RevEx.Desktop.Core.Interfaces;

public interface IAuthenticationService
{
    IReadOnlySet<string> Roles { get; }

    bool IsInRole(string role);

    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}

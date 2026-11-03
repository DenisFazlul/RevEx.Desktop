namespace RevEx.Desktop.Core.Interfaces;

public interface IAuthenticationService
{
#if NETSTANDARD2_0
    IReadOnlyCollection<string> Roles { get; }
#else
    IReadOnlySet<string> Roles { get; }
#endif

    bool IsInRole(string role);

    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}

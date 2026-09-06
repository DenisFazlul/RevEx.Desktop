namespace RevEx.Desktop.Core.Interfaces;

public interface IAuthenticationService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

using System.Net.Http.Headers;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Auth;

public sealed class AuthenticationHandler(IAuthenticationService authenticationService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await authenticationService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}

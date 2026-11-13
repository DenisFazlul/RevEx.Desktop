using RevEx.Desktop.Core.Interfaces;
using RevEx.Domain.Contracts.ContentVersion;

namespace RevEx.Desktop.Core.Services;

public sealed class RfaImportRepository(RevExApiTransport transport) : IRfaImportRepository
{
    public Task<ContentVersionDto> ImportAsync(
        RfaImportRequest request,
        CancellationToken cancellationToken = default)
    {
        return transport.ImportRfaAsync(request, cancellationToken);
    }
}

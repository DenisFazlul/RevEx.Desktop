using RevEx.Desktop.Core.Interfaces;
using RevEx.Domain.Contracts.ContentVersion;

namespace RevEx.Desktop.Core.Services;

public sealed class VersionImportRepository(RevExApiTransport transport) : IVersionImportRepository
{
    public Task<ContentVersionDto> ImportAsync(
        VersionImportRequest request,
        CancellationToken cancellationToken = default)
    {
        return transport.ImportVersionAsync(request, cancellationToken);
    }
}

using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using static RevEx.Desktop.Core.Services.MockApiData;

namespace RevEx.Desktop.Core.Services;

internal sealed class MockContentFileRepository : IContentFileRepository
{
    public Task<IReadOnlyCollection<ContentFileDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        FromCollection(ContentFiles, cancellationToken);

    public Task<ContentFileDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(ContentFiles.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<byte[]> DownloadAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ContentFiles.All(item => item.Id != id))
            throw new HttpRequestException($"File {id} was not found.");

        return Task.FromResult(Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
    }
}

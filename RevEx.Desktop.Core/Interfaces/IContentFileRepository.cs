using RevEx.Domain.Contracts.ContentFile;

namespace RevEx.Desktop.Core.Interfaces;

public interface IContentFileRepository
{
    Task<IReadOnlyCollection<ContentFileDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ContentFileDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<byte[]> DownloadAsync(int id, CancellationToken cancellationToken = default);
}

using RevEx.Domain.Contracts.ContentVersion;

namespace RevEx.Desktop.Core.Interfaces;

public interface IContentVersionRepository
{
    Task<IReadOnlyCollection<ContentVersionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ContentVersionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ContentVersionDto> CreateAsync(
        CreateContentVersionDto request,
        CancellationToken cancellationToken = default);

    Task<ContentVersionDto> UploadFileAsync(
        int versionId,
        Stream content,
        string fileName,
        string role,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(
        int versionId,
        int fileId,
        CancellationToken cancellationToken = default);
}

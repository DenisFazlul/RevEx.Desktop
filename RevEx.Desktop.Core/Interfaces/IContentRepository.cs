using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.Core.Interfaces;

public interface IContentRepository
{
    Task<IReadOnlyCollection<ContentDto>> GetAllAsync(
        ContentQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ContentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ContentDto> CreateAsync(
        CreateContentDto request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        UpdateContentDto request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<ContentDto> UploadPreviewAsync(
        int id,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}

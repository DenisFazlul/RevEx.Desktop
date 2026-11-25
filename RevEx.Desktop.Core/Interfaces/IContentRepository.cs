using RevEx.Domain.Contracts.Content;

namespace RevEx.Desktop.Core.Interfaces;

public interface IContentRepository
{
    Task<IReadOnlyCollection<ContentDto>> GetAllAsync(
        ContentQueryDto query,
        CancellationToken cancellationToken = default);

    Task SetFrozenAsync(int id, bool isFrozen, CancellationToken cancellationToken = default);

    Task<ContentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ContentDto?> GetByCidAsync(Guid cid, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ContentParameterDto>> GetParametersAsync(int contentId, CancellationToken token = default);
    Task<ContentParameterDto> CreateParameterAsync(int contentId, ContentParameterInput input, CancellationToken token = default);
    Task UpdateParameterAsync(int contentId, int id, ContentParameterInput input, CancellationToken token = default);
    Task DeleteParameterAsync(int contentId, int id, CancellationToken token = default);

    Task<Guid> GenerateUuid7Async(CancellationToken cancellationToken = default);

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

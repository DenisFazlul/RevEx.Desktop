using RevEx.Domain.Contracts.ContentSet;

namespace RevEx.Desktop.Core.Interfaces;

public interface IContentSetRepository
{
    Task<IReadOnlyCollection<ContentSetDto>> GetAllAsync(CancellationToken token = default);
    Task<ContentSetDto> CreateAsync(SaveContentSetDto request, CancellationToken token = default);
    Task UpdateAsync(int id, SaveContentSetDto request, CancellationToken token = default);
    Task DeleteAsync(int id, CancellationToken token = default);
    Task<IReadOnlyCollection<ContentSetContentDto>> GetContentsAsync(int id, CancellationToken token = default);
    Task AddContentAsync(int id, int contentId, CancellationToken token = default);
    Task RemoveContentAsync(int id, int contentId, CancellationToken token = default);
}

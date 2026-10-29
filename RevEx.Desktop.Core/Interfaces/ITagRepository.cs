using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.Core.Interfaces;

public interface ITagRepository
{
    Task<IReadOnlyCollection<TagDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TagDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TagDto> CreateAsync(CreateTagDto request, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, UpdateTagDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

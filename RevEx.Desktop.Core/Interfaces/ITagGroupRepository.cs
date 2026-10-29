using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.Core.Interfaces;

public interface ITagGroupRepository
{
    Task<IReadOnlyCollection<TagGroupDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TagGroupDto> CreateAsync(CreateTagGroupDto request, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, UpdateTagGroupDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

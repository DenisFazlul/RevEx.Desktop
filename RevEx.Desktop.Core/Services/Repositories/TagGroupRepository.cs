using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class TagGroupRepository(RevExApiTransport transport) : ITagGroupRepository
{
    public Task<IReadOnlyCollection<TagGroupDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<TagGroupDto>("api/tag-groups", cancellationToken);

    public Task<TagGroupDto> CreateAsync(CreateTagGroupDto request, CancellationToken cancellationToken = default) =>
        transport.PostAsJsonAsync<CreateTagGroupDto, TagGroupDto>("api/tag-groups", request, cancellationToken);

    public Task UpdateAsync(int id, UpdateTagGroupDto request, CancellationToken cancellationToken = default) =>
        transport.PutAsJsonAsync($"api/tag-groups/{id}", request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        transport.DeleteAsync($"api/tag-groups/{id}", cancellationToken);
}

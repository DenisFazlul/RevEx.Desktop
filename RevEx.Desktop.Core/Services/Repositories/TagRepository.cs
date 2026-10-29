using RevEx.Domain.Contracts.Tag;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class TagRepository(RevExApiTransport transport) : ITagRepository
{
    public Task<IReadOnlyCollection<TagDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<TagDto>("api/tags", cancellationToken);

    public Task<TagDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        transport.GetByIdAsync<TagDto>("api/tags", id, cancellationToken);

    public Task<TagDto> CreateAsync(CreateTagDto request, CancellationToken cancellationToken = default) =>
        transport.PostAsJsonAsync<CreateTagDto, TagDto>("api/tags", request, cancellationToken);

    public Task UpdateAsync(int id, UpdateTagDto request, CancellationToken cancellationToken = default) =>
        transport.PutAsJsonAsync($"api/tags/{id}", request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        transport.DeleteAsync($"api/tags/{id}", cancellationToken);
}

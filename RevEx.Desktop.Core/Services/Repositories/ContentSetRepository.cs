using RevEx.Desktop.Core.Interfaces;
using RevEx.Domain.Contracts.ContentSet;

namespace RevEx.Desktop.Core.Services;

public sealed class ContentSetRepository(RevExApiTransport transport) : IContentSetRepository
{
    public Task<IReadOnlyCollection<ContentSetDto>> GetAllAsync(CancellationToken token = default)
    {
        return transport.GetAllAsync<ContentSetDto>("api/content-sets", token);
    }

    public Task<ContentSetDto> CreateAsync(SaveContentSetDto request, CancellationToken token = default)
    {
        return transport.PostAsJsonAsync<SaveContentSetDto, ContentSetDto>("api/content-sets", request, token);
    }

    public Task UpdateAsync(int id, SaveContentSetDto request, CancellationToken token = default)
    {
        return transport.PutAsJsonAsync($"api/content-sets/{id}", request, token);
    }

    public Task DeleteAsync(int id, CancellationToken token = default)
    {
        return transport.DeleteAsync($"api/content-sets/{id}", token);
    }

    public Task<IReadOnlyCollection<ContentSetContentDto>> GetContentsAsync(int id, CancellationToken token = default)
    {
        return transport.GetAllAsync<ContentSetContentDto>($"api/content-sets/{id}/contents", token);
    }

    public Task AddContentAsync(int id, int contentId, CancellationToken token = default)
    {
        return transport.PutAsJsonAsync($"api/content-sets/{id}/contents/{contentId}", new { }, token);
    }

    public Task RemoveContentAsync(int id, int contentId, CancellationToken token = default)
    {
        return transport.DeleteAsync($"api/content-sets/{id}/contents/{contentId}", token);
    }
}

using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Services;

public sealed class CategoryRepository(RevExApiTransport transport) : ICategoryRepository
{
    public Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        transport.GetAllAsync<CategoryDto>("api/categories", cancellationToken);

    public Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        transport.GetByIdAsync<CategoryDto>("api/categories", id, cancellationToken);

    public Task<CategoryDto> CreateAsync(
        CreateCategoryDto request,
        CancellationToken cancellationToken = default) =>
        transport.PostAsJsonAsync<CreateCategoryDto, CategoryDto>("api/categories", request, cancellationToken);

    public Task UpdateAsync(
        int id,
        UpdateCategoryDto request,
        CancellationToken cancellationToken = default) =>
        transport.PutAsJsonAsync($"api/categories/{id}", request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        transport.DeleteAsync($"api/categories/{id}", cancellationToken);
}

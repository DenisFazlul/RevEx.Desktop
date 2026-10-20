using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using static RevEx.Desktop.Core.Services.MockApiData;

namespace RevEx.Desktop.Core.Services;

internal sealed class MockCategoryRepository : ICategoryRepository
{
    public Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        FromCollection(Categories, cancellationToken);

    public Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        FromItem(Categories.FirstOrDefault(item => item.Id == id), cancellationToken);

    public Task<CategoryDto> CreateAsync(
        CreateCategoryDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var category = new CategoryDto(
            Categories.Count == 0 ? 1 : Categories.Max(item => item.Id) + 1,
            request.Name,
            now,
            now);
        Categories.Add(category);
        return Task.FromResult(category);
    }

    public Task UpdateAsync(
        int id,
        UpdateCategoryDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = Categories.FindIndex(item => item.Id == id);
        if (index < 0)
            throw new HttpRequestException($"Category {id} was not found.");

        Categories[index] = Categories[index] with { Name = request.Name, UpdatedAt = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Categories.RemoveAll(item => item.Id == id);
        return Task.CompletedTask;
    }
}

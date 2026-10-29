using RevEx.Domain.Contracts.Category;

namespace RevEx.Desktop.Core.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyCollection<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(CreateCategoryDto request, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, UpdateCategoryDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

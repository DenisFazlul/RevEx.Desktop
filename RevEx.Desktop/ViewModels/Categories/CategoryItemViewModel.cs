using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.Category;

namespace RevEx.Desktop.ViewModels.Categories;

/// <summary>Модель категории в списке и фильтре. Отмечает системную категорию, которую нельзя менять.</summary>
public sealed class CategoryItemViewModel(CategoryDto dto) : FilterItemViewModel(dto.Id, dto.Name)
{
    public bool CanModify => Id != 1;
}

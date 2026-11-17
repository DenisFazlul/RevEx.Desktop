using System.Collections.Generic;
using System.Linq;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.Category;

namespace RevEx.Desktop.ViewModels.Categories;

/// <summary>Модель выбора категории карточки. Сортирует категории и восстанавливает текущий выбор.</summary>
public sealed class CategorySelectionViewModel : SelectionDialogViewModel<CategoryDto>
{
    public CategorySelectionViewModel(IEnumerable<CategoryDto> categories, int? selectedCategoryId)
        : base(categories.OrderBy(category => category.Name))
    {
        SelectedItem = Items.FirstOrDefault(category => category.Id == selectedCategoryId) ?? Items.FirstOrDefault();
    }
}

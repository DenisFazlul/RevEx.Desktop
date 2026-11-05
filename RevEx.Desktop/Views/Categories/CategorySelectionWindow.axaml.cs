using System.Collections.Generic;
using RevEx.Domain.Contracts.Category;
using RevEx.Desktop.ViewModels.Categories;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Categories;

public partial class CategorySelectionWindow : DialogWindow
{
    public CategorySelectionWindow() : this([], null)
    {
    }

    public CategorySelectionWindow(IEnumerable<CategoryDto> categories, int? selectedCategoryId)
    {
        InitializeComponent();
        DataContext = new CategorySelectionViewModel(categories, selectedCategoryId);
    }
}

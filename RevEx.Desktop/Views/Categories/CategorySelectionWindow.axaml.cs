using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.Views.Categories;

public partial class CategorySelectionWindow : Window
{
    public IReadOnlyCollection<CategoryDto> Categories { get; }
    public CategoryDto? SelectedCategory { get; set; }

    public CategorySelectionWindow() : this([], null) { }

    public CategorySelectionWindow(IEnumerable<CategoryDto> categories, int? selectedCategoryId)
    {
        Categories = categories.OrderBy(item => item.Name).ToArray();
        SelectedCategory = Categories.FirstOrDefault(item => item.Id == selectedCategoryId)
            ?? Categories.FirstOrDefault();
        InitializeComponent();
        DataContext = this;
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (CategoryBox.SelectedItem is CategoryDto category)
            Close(category);
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close(null);
}

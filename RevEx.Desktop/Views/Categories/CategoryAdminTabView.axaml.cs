using Avalonia.Controls;
using RevEx.Desktop.ViewModels.Categories;
using RevEx.Desktop.Views.Common;
using RevEx.Desktop.Views.Tags;

namespace RevEx.Desktop.Views.Categories;

public partial class CategoryAdminTabView : UserControl
{
    public CategoryAdminTabView() => InitializeComponent();

    private async void OnCreateClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!TryGetContext(out var owner, out var viewModel))
            return;

        var editor = CreateEditor("Новая категория", "Создание категории", string.Empty);
        var name = await editor.ShowDialog<string?>(owner);
        if (name is not null)
            await viewModel.CreateAsync(name);
    }

    private async void OnEditClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: CategoryItemViewModel category } ||
            !TryGetContext(out var owner, out var viewModel))
            return;

        var editor = CreateEditor("Редактирование категории", "Название категории", category.Name);
        var name = await editor.ShowDialog<string?>(owner);
        if (name is not null)
            await viewModel.RenameAsync(category, name);
    }

    private async void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: CategoryItemViewModel category } ||
            !TryGetContext(out var owner, out var viewModel))
            return;

        var confirmation = new DeleteConfirmationWindow(
            $"Категория «{category.Name}» будет удалена. Категорию, используемую контентом, удалить нельзя.");
        if (await confirmation.ShowDialog<bool>(owner))
            await viewModel.DeleteAsync(category);
    }

    private static TextInputWindow CreateEditor(string windowTitle, string formTitle, string value) =>
        new(new TextInputOptions(
            windowTitle,
            formTitle,
            "Название",
            "Введите название категории",
            value,
            MaximumLength: 200));

    private bool TryGetContext(out Window owner, out CategoryAdminTabViewModel viewModel)
    {
        owner = null!;
        viewModel = null!;
        if (DataContext is not CategoryAdminTabViewModel currentViewModel ||
            TopLevel.GetTopLevel(this) is not Window currentOwner)
            return false;

        owner = currentOwner;
        viewModel = currentViewModel;
        return true;
    }
}

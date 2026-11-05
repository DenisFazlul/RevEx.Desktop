using RevEx.Desktop.ViewModels.Common;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using RevEx.Desktop.Configuration;
using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Contracts.Tag;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Categories;
using RevEx.Desktop.Views.Common;
using RevEx.Desktop.Views.Connectors;
using RevEx.Desktop.Views.Tags;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentDetailsView : UserControl
{
    public ContentDetailsView()
    {
        InitializeComponent();
    }

    private async void OnAddTagClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var availableTags = await viewModel.GetAvailableTagsAsync();
        var window = new TagSelectionWindow(availableTags);
        var tag = await window.ShowDialog<TagDto?>(owner);
        if (tag is not null)
        {
            await viewModel.AssignTagAsync(tag);
        }
    }

    private async void OnUpdatePreviewClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel viewModel)
        {
            return;
        }

        var file = await LocalFilePicker.OpenAsync(this, "Выберите новое изображение контента", ContentImageUploadConfiguration.FilePickerType);
        if (file is not null)
        {
            await viewModel.UpdatePreviewAsync(file.Path, file.Name);
        }
    }

    private async void OnDeleteContentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var confirmation = new DeleteConfirmationWindow(
            $"Контент «{viewModel.Name}» и все его версии будут удалены. Продолжить?");
        if (await confirmation.ShowDialog<bool>(owner))
        {
            await viewModel.DeleteContentAsync();
        }
    }

    private async void OnEditNameClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var editor = new TextInputWindow(new TextInputOptions(
            "Редактирование названия",
            "Название контента",
            "Название",
            "Введите новое название карточки контента",
            viewModel.Name,
            MaximumLength: 200));
        var name = await editor.ShowDialog<string?>(owner);
        if (name is not null)
        {
            await viewModel.UpdateContentNameAsync(name);
        }
    }

    private async void OnEditDescriptionClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var editor = new TextInputWindow(new TextInputOptions(
            "Редактирование описания",
            "Описание контента",
            "Описание",
            "Введите новое описание карточки контента",
            viewModel.Description,
            IsMultiline: true));
        var description = await editor.ShowDialog<string?>(owner);
        if (description is not null)
        {
            await viewModel.UpdateContentDescriptionAsync(description);
        }
    }

    private async void OnEditCategoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var selector = new CategorySelectionWindow(viewModel.GetCategories(), viewModel.GetCurrentCategoryId());
        var category = await selector.ShowDialog<CategoryDto?>(owner);
        if (category is not null)
        {
            await viewModel.UpdateContentCategoryAsync(category);
        }
    }

    private async void OnAddVersionFileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel viewModel)
        {
            return;
        }

        var file = await LocalFilePicker.OpenAsync(this, "Выберите файл версии", FilePickerFileTypes.All);
        if (file is not null)
        {
            await viewModel.AddVersionFileAsync(file.Path, file.Name);
        }
    }

    private async void OnVersionClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is Control { DataContext: ContentVersionDto version } &&
            DataContext is ContentDetailsViewModel viewModel)
        {
            await viewModel.SelectVersionCommand.ExecuteAsync(version);
        }
    }

    private async void OnDeleteVersionFileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: ContentVersionFileItemViewModel file } ||
            !this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var confirmation = new DeleteConfirmationWindow(
            $"Файл «{file.Name}» будет удалён из версии. Продолжить?");
        if (await confirmation.ShowDialog<bool>(owner))
        {
            await viewModel.DeleteVersionFileAsync(file);
        }
    }

    private async void OnLoadVersionIntoProjectClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var selector = new ConnectorSelectionWindow(viewModel.GetAvailableConnectors());
        var connector = await selector.ShowDialog<RevEx.Domain.Contracts.Connectors.ConnectorDescriptor?>(owner);
        if (connector is not null)
        {
            viewModel.QueueVersionForLoading(connector);
        }
    }

    private async void OnShowHistoryDetailsClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: ContentHistoryEventViewModel historyEvent } ||
            TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var window = new ContentHistoryDetailsWindow(historyEvent.Value);
        await window.ShowDialog(owner);
    }
}

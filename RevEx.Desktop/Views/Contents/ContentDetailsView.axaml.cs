using RevEx.Desktop.ViewModels.Common;
using Avalonia.Controls;
using Avalonia.Input.Platform;
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
using RevEx.Desktop.IfcViewer;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentDetailsView : UserControl
{
    public ContentDetailsView()
    {
        InitializeComponent();
    }

    private async void OnAddToSetClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel content ||
            TopLevel.GetTopLevel(this) is not Window { DataContext: RevEx.Desktop.ViewModels.MainViewModel main } owner)
        {
            return;
        }

        var window = new ContentSetsWindow(main.CreateContentSetsDialog(content.Id));
        await window.ShowDialog(owner);
    }

    private async void OnCopyIdClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentDetailsViewModel viewModel)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            CopyIdStatus.Text = "Буфер обмена недоступен.";
            return;
        }

        try
        {
            await clipboard.SetTextAsync(viewModel.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
            CopyIdStatus.Text = "ID скопирован.";
        }
        catch (System.Exception exception) when (exception is System.InvalidOperationException or
                                                   System.IO.IOException or System.UnauthorizedAccessException)
        {
            CopyIdStatus.Text = "Не удалось скопировать ID.";
        }
    }

    private async void OnAddParameterClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        viewModel.AddParameterCommand.Execute(null);
        if (viewModel.ParameterEditor is { } editor)
        {
            var window = new ContentParameterEditorWindow(viewModel, editor);
            await window.ShowDialog(owner);
        }
    }

    private async void OnEditParameterClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: RevEx.Domain.Contracts.Content.ContentParameterDto parameter } ||
            !this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        viewModel.EditParameterCommand.Execute(parameter);
        if (viewModel.ParameterEditor is { } editor)
        {
            var window = new ContentParameterEditorWindow(viewModel, editor);
            await window.ShowDialog(owner);
        }
    }

    private async void OnAddTagClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var availableTags = await viewModel.GetAvailableTagsAsync();
        var window = new TagSelectionWindow(availableTags);
        var tags = await window.ShowDialog<IReadOnlyList<TagDto>?>(owner);
        if (tags is not null)
        {
            await viewModel.AssignTagsAsync(tags);
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

    private async void OnAddVersionClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (!this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var window = new VersionImportWindow(viewModel.CreateVersionImport());
        var version = await window.ShowDialog<ContentVersionDto?>(owner);

        if (version is not null)
        {
            viewModel.AddOrUpdateVersion(version);
        }
    }

    private async void OnVersionSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (sender is ComboBox { SelectedItem: ContentVersionDto version } &&
            DataContext is ContentDetailsViewModel viewModel &&
            viewModel.SelectedVersion?.Id != version.Id)
        {
            await viewModel.SelectVersionCommand.ExecuteAsync(version);
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

    private async void OnDownloadVersionFileClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: ContentVersionFileItemViewModel file } ||
            DataContext is not ContentDetailsViewModel viewModel)
        {
            return;
        }

        var path = await LocalFilePicker.SaveAsync(this, "Сохранить файл версии", file.Name);
        if (path is not null)
        {
            await viewModel.DownloadVersionFileAsync(file, path);
        }
    }

    private async void OnPreviewIfcFileClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (sender is not Control { DataContext: ContentVersionFileItemViewModel file } ||
            !file.IsIfc ||
            !this.TryGetContext<ContentDetailsViewModel>(out var owner, out var viewModel))
        {
            return;
        }

        var content = await viewModel.DownloadVersionFileForPreviewAsync(file);
        if (content is null)
        {
            return;
        }

        var viewer = new IfcViewerWindow(file.Name, content);
        viewer.Show(owner);
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

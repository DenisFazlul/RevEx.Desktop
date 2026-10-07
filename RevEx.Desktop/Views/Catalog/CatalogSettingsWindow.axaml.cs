using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using RevEx.Desktop.ViewModels.Catalog;

namespace RevEx.Desktop.Views.Catalog;

public partial class CatalogSettingsWindow : Window
{
    private readonly CatalogTabViewModel _catalog;
    private readonly HashSet<int> _selectedCategoryIds;
    private readonly HashSet<int> _selectedTagIds;
    private bool _accepted;

    public CatalogSettingsWindow()
    {
        InitializeComponent();
        _catalog = null!;
        _selectedCategoryIds = [];
        _selectedTagIds = [];
    }

    public CatalogSettingsWindow(CatalogTabViewModel catalog)
    {
        InitializeComponent();
        _catalog = catalog;
        DataContext = catalog;
        _selectedCategoryIds = catalog.Categories.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();
        _selectedTagIds = catalog.Tags.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();
    }

    private void OnResetClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        foreach (var category in _catalog.Categories)
            category.IsSelected = false;
        foreach (var tag in _catalog.Tags)
            tag.IsSelected = false;
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        Close(false);
    }

    private void OnApplyClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        _accepted = true;
        Close(true);
    }

    protected override void OnClosing(WindowClosingEventArgs eventArgs)
    {
        if (!_accepted && _catalog is not null)
        {
            foreach (var category in _catalog.Categories)
                category.IsSelected = _selectedCategoryIds.Contains(category.Id);
            foreach (var tag in _catalog.Tags)
                tag.IsSelected = _selectedTagIds.Contains(tag.Id);
        }

        base.OnClosing(eventArgs);
    }
}

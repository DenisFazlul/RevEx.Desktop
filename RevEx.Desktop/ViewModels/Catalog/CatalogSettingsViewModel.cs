using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.ViewModels.Common;

namespace RevEx.Desktop.ViewModels.Catalog;

/// <summary>Модель окна фильтров. Хранит черновой выбор и применяет его к каталогу только по подтверждению.</summary>
public partial class CatalogSettingsViewModel : DialogViewModel<bool>
{
    private readonly CatalogTabViewModel _catalog;

    [ObservableProperty]
    private string? _categorySearchText;

    [ObservableProperty]
    private string? _tagSearchText;

    [ObservableProperty]
    private IReadOnlyList<CatalogFilterGroupViewModel> _visibleTagGroups;

    public CatalogSettingsViewModel(CatalogTabViewModel catalog) : base(false)
    {
        _catalog = catalog;
        Categories = new CatalogFilterGroupViewModel("Категории", catalog.Categories);
        TagGroups = catalog.Tags
            .GroupBy(item => item.GroupId)
            .Select(group => new CatalogFilterGroupViewModel(group.First().GroupName, group.OrderBy(item => item.Name)))
            .OrderBy(group => group.Name)
            .ToArray();
        _visibleTagGroups = TagGroups;
    }

    public CatalogFilterGroupViewModel Categories { get; }

    public IReadOnlyList<CatalogFilterGroupViewModel> TagGroups { get; }

    partial void OnCategorySearchTextChanged(string? value)
    {
        Categories.FilterItems(value?.Trim() ?? string.Empty);
    }

    partial void OnTagSearchTextChanged(string? value)
    {
        var searchText = value?.Trim() ?? string.Empty;
        foreach (var group in TagGroups)
        {
            group.FilterItems(searchText);
        }

        VisibleTagGroups = TagGroups.Where(group => group.VisibleItems.Count > 0).ToArray();
    }

    [RelayCommand]
    private void Reset()
    {
        Categories.SetSelection(false);
        foreach (var group in TagGroups)
        {
            group.SetSelection(false);
        }
    }

    [RelayCommand]
    private void Apply()
    {
        var categoryIds = Categories.Items.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();
        var tagIds = TagGroups.SelectMany(group => group.Items).Where(item => item.IsSelected)
            .Select(item => item.Id).ToHashSet();
        _catalog.ApplyFilters(categoryIds, tagIds);
        Complete(true);
    }
}

public sealed partial class CatalogFilterGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<FilterItemViewModel> _visibleItems;

    [ObservableProperty]
    private bool _isSearchActive;

    public CatalogFilterGroupViewModel(string name, IEnumerable<FilterItemViewModel> items)
    {
        Name = name;
        Items = items.Select(item => new FilterItemViewModel(item.Id, item.Name, item.IsSelected, item.ColorBrush)).ToArray();
        _visibleItems = Items;
        foreach (var item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        UpdateSelectedCount();
    }

    public string Name { get; }

    public IReadOnlyList<FilterItemViewModel> Items { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private int _selectedCount;

    public bool HasSelection => SelectedCount > 0;

    public string DisplayName => HasSelection ? $"{Name} ({SelectedCount})" : Name;

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilterItemViewModel.IsSelected))
        {
            UpdateSelectedCount();
        }
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Items.Count(item => item.IsSelected);
    }

    internal void FilterItems(string searchText)
    {
        IsSearchActive = searchText.Length > 0;
        VisibleItems = IsSearchActive
            ? Items.Where(item => item.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToArray()
            : Items;
    }

    [RelayCommand]
    private void SelectAll()
    {
        SetSelection(VisibleItems, true);
    }

    [RelayCommand]
    private void Clear()
    {
        SetSelection(VisibleItems, false);
    }

    internal void SetSelection(bool isSelected)
    {
        SetSelection(Items, isSelected);
    }

    private static void SetSelection(IEnumerable<FilterItemViewModel> items, bool isSelected)
    {
        foreach (var item in items)
        {
            item.IsSelected = isSelected;
        }
    }
}

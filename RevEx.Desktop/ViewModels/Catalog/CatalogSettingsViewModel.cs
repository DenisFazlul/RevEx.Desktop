using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.ViewModels.Common;

namespace RevEx.Desktop.ViewModels.Catalog;

/// <summary>Модель окна фильтров. Хранит черновой выбор и применяет его к каталогу только по подтверждению.</summary>
public partial class CatalogSettingsViewModel : DialogViewModel<bool>
{
    private readonly CatalogTabViewModel _catalog;

    public CatalogSettingsViewModel(CatalogTabViewModel catalog) : base(false)
    {
        _catalog = catalog;
        Categories = catalog.Categories.Select(item => new FilterItemViewModel(item.Id, item.Name, item.IsSelected)).ToArray();
        Tags = catalog.Tags.Select(item => new FilterItemViewModel(item.Id, item.Name, item.IsSelected)).ToArray();
    }

    public IReadOnlyList<FilterItemViewModel> Categories
    {
        get;
    }
    public IReadOnlyList<FilterItemViewModel> Tags
    {
        get;
    }

    [RelayCommand]
    private void Reset()
    {
        foreach (var item in Categories.Concat(Tags))
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    private void Apply()
    {
        var categoryIds = Categories.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();
        var tagIds = Tags.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();
        _catalog.ApplyFilters(categoryIds, tagIds);
        Complete(true);
    }
}

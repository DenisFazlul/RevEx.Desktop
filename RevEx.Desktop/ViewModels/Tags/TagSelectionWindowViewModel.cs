using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.ViewModels.Tags;

public sealed partial class TagSelectionWindowViewModel : DialogViewModel<IReadOnlyList<TagDto>>
{
    private readonly IReadOnlyList<TagDto> _tags;
    private readonly IReadOnlyList<CatalogFilterGroupViewModel> _groups;

    [ObservableProperty]
    private string? _tagSearchText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoMatches))]
    private IReadOnlyList<CatalogFilterGroupViewModel> _visibleTagGroups;

    public TagSelectionWindowViewModel(IEnumerable<TagDto> tags)
    {
        _tags = tags.ToArray();
        _groups = _tags
            .GroupBy(tag => tag.TagGroupId)
            .Select(group => new CatalogFilterGroupViewModel(
                group.First().TagGroupName,
                group.OrderBy(tag => tag.Name).Select(tag => new TagFilterItemViewModel(tag))))
            .OrderBy(group => group.Name)
            .ToArray();
        _visibleTagGroups = _groups;

        foreach (var group in _groups)
        {
            group.PropertyChanged += OnGroupPropertyChanged;
        }
    }

    public bool HasTags => _tags.Count > 0;

    public bool HasNoMatches => HasTags && VisibleTagGroups.Count == 0;

    partial void OnTagSearchTextChanged(string? value)
    {
        var searchText = value?.Trim() ?? string.Empty;
        foreach (var group in _groups)
        {
            group.FilterItems(searchText);
        }

        VisibleTagGroups = _groups.Where(group => group.VisibleItems.Count > 0).ToArray();
    }

    private void OnGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogFilterGroupViewModel.SelectedCount))
        {
            AcceptCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanAccept() => _groups.Any(group => group.HasSelection);

    [RelayCommand(CanExecute = nameof(CanAccept))]
    private void Accept()
    {
        var selectedIds = _groups.SelectMany(group => group.Items)
            .Where(item => item.IsSelected)
            .Select(item => item.Id)
            .ToHashSet();
        var selectedTags = _tags.Where(tag => selectedIds.Contains(tag.Id)).ToArray();
        Complete(selectedTags);
    }
}

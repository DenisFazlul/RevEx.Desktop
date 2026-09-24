using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.ViewModels.Tags;

public partial class TagGroupItemViewModel : ViewModelBase
{
    private readonly Action<TagGroupItemViewModel> _edit;
    private readonly Action<TagGroupItemViewModel, TagItemViewModel?> _selectTag;

    public int Id { get; }
    public ObservableCollection<TagItemViewModel> Tags { get; } = [];
    public int TagCount => Tags.Count;
    public bool HasTags => Tags.Count > 0;
    public bool HasNoTags => !HasTags;

    [ObservableProperty] private string _name;
    [ObservableProperty] private TagItemViewModel? _selectedTag;

    public TagGroupItemViewModel(
        TagGroupDto dto,
        Action<TagGroupItemViewModel> edit,
        Action<TagGroupItemViewModel, TagItemViewModel?> selectTag)
    {
        Id = dto.Id;
        _name = dto.Name;
        _edit = edit;
        _selectTag = selectTag;
    }

    [RelayCommand]
    private void Edit() => _edit(this);

    partial void OnSelectedTagChanged(TagItemViewModel? value) => _selectTag(this, value);

    public void NotifyTagCountChanged()
    {
        OnPropertyChanged(nameof(TagCount));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(HasNoTags));
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Модель строки группы тегов. Хранит название и теги, автоматически обновляет счётчик.</summary>
public partial class TagGroupItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;

    public TagGroupItemViewModel(TagGroupDto dto)
    {
        Id = dto.Id;
        _name = dto.Name;
        Tags.CollectionChanged += (_, _) => NotifyTagCountChanged();
    }

    public int Id
    {
        get;
    }
    public ObservableCollection<TagItemViewModel> Tags { get; } = [];
    public int TagCount => Tags.Count;
    public bool HasTags => Tags.Count > 0;
    public bool HasNoTags => !HasTags;

    private void NotifyTagCountChanged()
    {
        OnPropertyChanged(nameof(TagCount));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(HasNoTags));
    }
}

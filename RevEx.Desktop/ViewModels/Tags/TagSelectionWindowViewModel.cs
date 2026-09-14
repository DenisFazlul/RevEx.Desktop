using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.ViewModels.Tags;

public partial class TagSelectionWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private TagDto? _selectedTag;

    public ObservableCollection<TagDto> Tags { get; }
    public bool HasTags => Tags.Count > 0;

    public TagSelectionWindowViewModel(IEnumerable<TagDto> tags) =>
        Tags = new ObservableCollection<TagDto>(tags);
}

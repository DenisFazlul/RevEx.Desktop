using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.ViewModels.Tags;

public partial class TagFilterItemViewModel : ViewModelBase
{
    public int Id { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isSelected;

    public TagFilterItemViewModel(TagDto dto)
    {
        Id = dto.Id;
        _name = dto.Name;
    }
}

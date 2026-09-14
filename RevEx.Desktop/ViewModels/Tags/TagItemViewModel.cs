using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.ViewModels.Tags;

public partial class TagItemViewModel : ViewModelBase
{
    public int Id { get; }

    [ObservableProperty]
    private string _name;

    public TagItemViewModel(TagDto dto)
    {
        Id = dto.Id;
        _name = dto.Name;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentItemViewModel : ViewModelBase
{
    public int Id { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    public ContentItemViewModel(ContentDto dto)
    {
        Id = dto.Id;
        _name = dto.Name;
        _description = dto.Description;
    }
}

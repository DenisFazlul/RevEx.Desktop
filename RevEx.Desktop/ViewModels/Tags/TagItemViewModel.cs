using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Модель строки тега в справочнике. Хранит название и связь с группой.</summary>
public partial class TagItemViewModel : ViewModelBase
{
    public int Id
    {
        get;
    }
    public int TagGroupId
    {
        get; set;
    }

    [ObservableProperty]
    private string _name;

    public TagItemViewModel(TagDto dto)
    {
        Id = dto.Id;
        TagGroupId = dto.TagGroupId;
        _name = dto.Name;
    }
}

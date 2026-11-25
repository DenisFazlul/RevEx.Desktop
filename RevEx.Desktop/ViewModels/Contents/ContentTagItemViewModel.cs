using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Модель тега на карточке. Показывает название и предоставляет команду снятия тега.</summary>
public partial class ContentTagItemViewModel : ViewModelBase
{
    private readonly Func<ContentTagItemViewModel, Task> _remove;

    public int Id
    {
        get;
    }

    public string Name
    {
        get;
    }
    public Avalonia.Media.IBrush? ColorBrush { get; }

    public bool CanEdit
    {
        get;
    }

    public ContentTagItemViewModel(
        TagDto dto,
        Func<ContentTagItemViewModel, Task> remove,
        bool canEdit = true)
    {
        Id = dto.Id;
        Name = dto.Name;
        ColorBrush = Tags.TagColorBrush.FromHex(dto.Color);
        CanEdit = canEdit;
        _remove = remove;
    }

    [RelayCommand]
    private Task RemoveAsync() => _remove(this);
}

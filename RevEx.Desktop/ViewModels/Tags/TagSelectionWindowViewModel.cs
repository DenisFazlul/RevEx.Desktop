using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Domain.Contracts.Tag;
using RevEx.Desktop.ViewModels.Common;

namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Модель выбора тега для карточки. Возвращает выбранный тег без изменения каталога.</summary>
public class TagSelectionWindowViewModel : SelectionDialogViewModel<TagDto>
{
    public IReadOnlyList<TagDto> Tags => Items;
    public bool HasTags => HasItems;

    public TagSelectionWindowViewModel(IEnumerable<TagDto> tags) : base(tags)
    {
    }
}

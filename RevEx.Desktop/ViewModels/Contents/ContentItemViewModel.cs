using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentItemViewModel : ViewModelBase
{
    public int Id { get; }
    public int CategoryId { get; }
    public int ContentStatusId { get; }
    public IReadOnlyCollection<int> TagIds { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    public string TagsText { get; }

    public ContentItemViewModel(ContentDto dto, IReadOnlyDictionary<int, string>? tagNames = null)
    {
        Id = dto.Id;
        CategoryId = dto.CategoryId;
        ContentStatusId = dto.ContentStatusId;
        TagIds = dto.TagIds;
        _name = dto.Name;
        _description = dto.Description;
        TagsText = string.Join("  ", dto.TagIds
            .Select(id => tagNames is not null && tagNames.TryGetValue(id, out var name) ? $"#{name}" : $"#{id}"));
    }
}

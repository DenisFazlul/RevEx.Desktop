using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Configuration;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Domain.Contracts.Content;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Модель карточки в каталоге. Хранит краткие сведения, теги и изображение предпросмотра.</summary>
public partial class ContentItemViewModel : ViewModelBase, IDisposable
{
    public int Id
    {
        get;
    }
    public Guid Cid
    {
        get;
    }
    [ObservableProperty]
    private int _categoryId;
    public int ContentStatusId
    {
        get;
    }
    public IReadOnlyCollection<int> TagIds
    {
        get;
    }
    public IReadOnlyCollection<FilterItemViewModel> VisibleTags
    {
        get;
    }
    public IReadOnlyCollection<FilterItemViewModel> AllTags { get; }

    public string? AdditionalTagsText
    {
        get;
    }
    public bool HasAdditionalTags => AdditionalTagsText is not null;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    public Bitmap? PreviewImage
    {
        get;
    }
    public bool HasPreview => PreviewImage is not null;

    public ContentItemViewModel(
        ContentDto dto,
        IReadOnlyDictionary<int, TagFilterItemViewModel>? catalogTags = null,
        byte[]? previewBytes = null)
    {
        Id = dto.Id;
        Cid = dto.Cid;
        _categoryId = dto.CategoryId;
        ContentStatusId = dto.ContentStatusId;
        TagIds = dto.TagIds;
        _name = dto.Name;
        _description = dto.Description;
        var tags = dto.TagIds
            .Select(id => catalogTags is not null && catalogTags.TryGetValue(id, out var tag) ? tag : new FilterItemViewModel(id, id.ToString()))
            .ToArray();
        AllTags = tags;
        var visibleTagCount = CatalogDisplayConfiguration.GetMaximumVisibleTags();
        VisibleTags = tags.Take(visibleTagCount).ToArray();
        var additionalTagCount = tags.Length - VisibleTags.Count;
        AdditionalTagsText = additionalTagCount > 0 ? $"+{additionalTagCount}" : null;

        if (previewBytes is { Length: > 0 })
        {
            using var stream = new MemoryStream(previewBytes);
            PreviewImage = new Bitmap(stream);
        }
    }

    public void Dispose() => PreviewImage?.Dispose();
}

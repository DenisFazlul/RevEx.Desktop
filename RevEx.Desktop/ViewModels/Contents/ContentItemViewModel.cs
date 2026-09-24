using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Configuration;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.ViewModels.Contents;

public partial class ContentItemViewModel : ViewModelBase, IDisposable
{
    public int Id { get; }
    public int CategoryId { get; }
    public int ContentStatusId { get; }
    public IReadOnlyCollection<int> TagIds { get; }
    public IReadOnlyCollection<string> VisibleTags { get; }
    public string? AdditionalTagsText { get; }
    public bool HasAdditionalTags => AdditionalTagsText is not null;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _description;

    public Bitmap? PreviewImage { get; }
    public bool HasPreview => PreviewImage is not null;

    public ContentItemViewModel(
        ContentDto dto,
        IReadOnlyDictionary<int, string>? tagNames = null,
        byte[]? previewBytes = null)
    {
        Id = dto.Id;
        CategoryId = dto.CategoryId;
        ContentStatusId = dto.ContentStatusId;
        TagIds = dto.TagIds;
        _name = dto.Name;
        _description = dto.Description;
        var tags = dto.TagIds
            .Select(id => tagNames is not null && tagNames.TryGetValue(id, out var name) ? name : id.ToString())
            .ToArray();
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

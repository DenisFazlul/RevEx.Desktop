using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Categories;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Catalog;

public partial class CatalogTabViewModel : WorkspaceTabViewModel
{
    private readonly IRevExApiService _revExApiService;
    private readonly Action<ContentItemViewModel> _openContent;
    private CancellationTokenSource? _contentLoadCancellation;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isContentLoading;
    [ObservableProperty] private string _contentNameQuery = string.Empty;

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = [];
    public ObservableCollection<TagFilterItemViewModel> Tags { get; } = [];
    public ObservableCollection<ContentItemViewModel> Contents { get; } = [];

    public CatalogTabViewModel(IRevExApiService revExApiService, Action<ContentItemViewModel> openContent)
        : base("Каталог", true)
    {
        _revExApiService = revExApiService;
        _openContent = openContent;
    }

    public override Task ActivateAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var selectedCategoryIds = Categories.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();
            var selectedTagIds = Tags.Where(item => item.IsSelected).Select(item => item.Id).ToHashSet();
            var categoriesTask = _revExApiService.GetCategoriesAsync();
            var tagsTask = _revExApiService.GetTagsAsync();
            await Task.WhenAll(categoriesTask, tagsTask);
            var categoryDtos = await categoriesTask;
            var tagDtos = await tagsTask;

            foreach (var category in Categories)
                category.PropertyChanged -= OnCategoryPropertyChanged;

            Categories.Clear();
            foreach (var dto in categoryDtos)
            {
                var category = new CategoryItemViewModel(dto) { IsSelected = selectedCategoryIds.Contains(dto.Id) };
                category.PropertyChanged += OnCategoryPropertyChanged;
                Categories.Add(category);
            }

            foreach (var tag in Tags)
                tag.PropertyChanged -= OnTagPropertyChanged;

            Tags.Clear();
            foreach (var dto in tagDtos.OrderBy(item => item.Name))
            {
                var tag = new TagFilterItemViewModel(dto) { IsSelected = selectedTagIds.Contains(dto.Id) };
                tag.PropertyChanged += OnTagPropertyChanged;
                Tags.Add(tag);
            }

            ScheduleContentLoad();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void OpenContent(ContentItemViewModel content) => _openContent(content);

    partial void OnContentNameQueryChanged(string value) => ScheduleContentLoad();

    private void OnCategoryPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(CategoryItemViewModel.IsSelected))
            ScheduleContentLoad();
    }

    private void OnTagPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(TagFilterItemViewModel.IsSelected))
            ScheduleContentLoad();
    }

    private void ScheduleContentLoad()
    {
        _contentLoadCancellation?.Cancel();
        Contents.Clear();
        IsContentLoading = false;

        var categoryIds = Categories.Where(category => category.IsSelected).Select(category => category.Id).ToArray();
        if (categoryIds.Length == 0)
            return;

        var tagIds = Tags.Where(tag => tag.IsSelected).Select(tag => tag.Id).ToArray();
        var query = new ContentQueryDto { CategoryIds = categoryIds, TagIds = tagIds, Name = ContentNameQuery };
        var cancellation = new CancellationTokenSource();
        _contentLoadCancellation = cancellation;
        _ = LoadContentsAsync(query, cancellation);
    }

    private async Task LoadContentsAsync(ContentQueryDto query, CancellationTokenSource cancellation)
    {
        try
        {
            IsContentLoading = true;
            await Task.Delay(250, cancellation.Token);
            var contentDtos = await _revExApiService.GetContentsAsync(query, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();

            if (!ReferenceEquals(_contentLoadCancellation, cancellation))
                return;

            var tagNames = Tags.ToDictionary(tag => tag.Id, tag => tag.Name);
            foreach (var dto in ApplyQuery(contentDtos, query))
            {
                cancellation.Token.ThrowIfCancellationRequested();
                Contents.Add(new ContentItemViewModel(dto, tagNames));
            }
        }
        catch (OperationCanceledException)
        {
            // Быстрое изменение фильтров отменяет уже неактуальную загрузку.
        }
        finally
        {
            if (ReferenceEquals(_contentLoadCancellation, cancellation))
            {
                _contentLoadCancellation = null;
                IsContentLoading = false;
            }

            cancellation.Dispose();
        }
    }

    private static IEnumerable<ContentDto> ApplyQuery(
        IEnumerable<ContentDto> contents,
        ContentQueryDto query)
    {
        var result = contents;

        if (query.CategoryIds.Length > 0)
        {
            var categoryIds = query.CategoryIds.ToHashSet();
            result = result.Where(content => categoryIds.Contains(content.CategoryId));
        }

        if (query.TagIds.Length > 0)
        {
            var tagIds = query.TagIds.ToHashSet();
            result = result.Where(content =>
                tagIds.All(tagId => content.TagIds.Contains(tagId)));
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim();
            result = result.Where(content =>
                content.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        return result;
    }
}

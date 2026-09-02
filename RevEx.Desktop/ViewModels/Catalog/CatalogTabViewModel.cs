using System;
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
    public ObservableCollection<ContentItemViewModel> Contents { get; } = [];

    public CatalogTabViewModel(IRevExApiService revExApiService, Action<ContentItemViewModel> openContent)
        : base("Каталог", false)
    {
        _revExApiService = revExApiService;
        _openContent = openContent;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var categoryDtos = await _revExApiService.GetCategoriesAsync();

            foreach (var category in Categories)
                category.PropertyChanged -= OnCategoryPropertyChanged;

            Categories.Clear();
            foreach (var dto in categoryDtos)
            {
                var category = new CategoryItemViewModel(dto);
                category.PropertyChanged += OnCategoryPropertyChanged;
                Categories.Add(category);
            }
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

    private void ScheduleContentLoad()
    {
        _contentLoadCancellation?.Cancel();
        Contents.Clear();
        IsContentLoading = false;

        var categoryIds = Categories.Where(category => category.IsSelected).Select(category => category.Id).ToArray();
        if (categoryIds.Length == 0)
            return;

        var query = new ContentQueryDto { CategoryIds = categoryIds, Name = ContentNameQuery };
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

            foreach (var dto in contentDtos)
                Contents.Add(new ContentItemViewModel(dto));
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
}

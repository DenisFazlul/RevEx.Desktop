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

namespace RevEx.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IRevExApiService _revExApiService;
    private CancellationTokenSource? _contentLoadCancellation;

    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isContentLoading;

    [ObservableProperty]
    private string _contentNameQuery = string.Empty;

    [ObservableProperty]
    private ContentDetailsViewModel? _selectedTab;

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = [];
    public ObservableCollection<ContentItemViewModel> Contents { get; } = [];
    public ObservableCollection<ContentDetailsViewModel> Tabs { get; } = [];

    public MainViewModel(IRevExApiService revExApiService)
    {
        _revExApiService = revExApiService;
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var categoryDtos = await _revExApiService.GetCategoriesAsync();

            foreach (var category in Categories)
            {
                category.PropertyChanged -= OnCategoryPropertyChanged;
            }

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

    partial void OnContentNameQueryChanged(string value) => ScheduleContentLoad();

    private void OnCategoryPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(CategoryItemViewModel.IsSelected))
        {
            ScheduleContentLoad();
        }
    }

    private void ScheduleContentLoad()
    {
        _contentLoadCancellation?.Cancel();
        Contents.Clear();
        IsContentLoading = false;

        var categoryIds = Categories
            .Where(category => category.IsSelected)
            .Select(category => category.Id)
            .ToArray();

        if (categoryIds.Length == 0)
        {
            return;
        }

        var query = new ContentQueryDto
        {
            CategoryIds = categoryIds,
            Name = ContentNameQuery
        };
        var cancellation = new CancellationTokenSource();
        _contentLoadCancellation = cancellation;
        _ = LoadContentsAsync(query, cancellation);
    }

    public void OpenContent(ContentItemViewModel content)
    {
        var tab = Tabs.FirstOrDefault(item => item.Id == content.Id);

        if (tab is null)
        {
            tab = new ContentDetailsViewModel(content);
            Tabs.Add(tab);
        }

        SelectedTab = tab;
    }

    public void CloseTab(ContentDetailsViewModel tab)
    {
        var tabIndex = Tabs.IndexOf(tab);
        if (tabIndex < 0)
        {
            return;
        }

        Tabs.RemoveAt(tabIndex);

        if (ReferenceEquals(SelectedTab, tab))
        {
            SelectedTab = Tabs.Count == 0
                ? null
                : Tabs[Math.Min(tabIndex, Tabs.Count - 1)];
        }
    }

    private async Task LoadContentsAsync(ContentQueryDto query, CancellationTokenSource cancellation)
    {
        try
        {
            IsContentLoading = true;
            await Task.Delay(250, cancellation.Token);
            var contentDtos = await _revExApiService.GetContentsAsync(query, cancellation.Token);

            foreach (var dto in contentDtos)
            {
                Contents.Add(new ContentItemViewModel(dto));
            }
        }
        catch (OperationCanceledException)
        {
            // Быстрое переключение категории отменяет уже неактуальную загрузку.
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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Categories;

public partial class CategoryAdminTabViewModel(IRevExApiService apiService)
    : WorkspaceTabViewModel("Категории", true, "categories")
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string? _message;
    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = [];
    public bool HasCategories => Categories.Count > 0;
    public bool HasNoCategories => !HasCategories && !IsLoading;

    public override async Task ActivateAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var categories = await apiService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var category in categories.OrderBy(item => item.Name))
                Categories.Add(new CategoryItemViewModel(category));
            NotifyStateChanged();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось загрузить категории: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public Task<bool> CreateAsync(string name) => RunAsync(async () =>
    {
        var created = await apiService.CreateCategoryAsync(new CreateCategoryDto { Name = name.Trim() });
        Categories.Add(new CategoryItemViewModel(created));
        SortCategories();
        Message = "Категория создана.";
    });

    public Task<bool> RenameAsync(CategoryItemViewModel category, string name) => RunAsync(async () =>
    {
        await apiService.UpdateCategoryAsync(category.Id, new UpdateCategoryDto { Name = name.Trim() });
        category.Name = name.Trim();
        SortCategories();
        Message = "Категория переименована.";
    });

    public Task<bool> DeleteAsync(CategoryItemViewModel category) => RunAsync(async () =>
    {
        await apiService.DeleteCategoryAsync(category.Id);
        Categories.Remove(category);
        Message = "Категория удалена.";
        NotifyStateChanged();
    });

    partial void OnIsLoadingChanged(bool value) => NotifyStateChanged();

    private async Task<bool> RunAsync(Func<Task> action)
    {
        try
        {
            IsSaving = true;
            ErrorMessage = null;
            Message = null;
            await action();
            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = $"Не удалось изменить категории: {exception.Message}";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void SortCategories()
    {
        var sorted = Categories.OrderBy(item => item.Name).ToArray();
        Categories.Clear();
        foreach (var category in sorted)
            Categories.Add(category);
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(HasNoCategories));
    }
}

using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.ViewModels.Tabs;
using RevEx.Domain.Contracts.Category;

namespace RevEx.Desktop.ViewModels.Categories;

/// <summary>Модель справочника категорий. Загружает список и выполняет создание, переименование и удаление.</summary>
public class CategoryAdminTabViewModel(IRevExApiService apiService)
    : WorkspaceTabViewModel("Категории", true, "categories")
{
    public ObservableCollection<CategoryItemViewModel> Categories { get; } = [];
    public bool HasCategories => Categories.Count > 0;
    public bool HasNoCategories => !HasCategories && !IsLoading;

    public override async Task ActivateAsync()
    {
        await RunLoadAsync(async () =>
        {
            var categories = await apiService.Categories.GetAllAsync();
            Categories.ReplaceWith(categories.OrderBy(category => category.Name).Select(category => new CategoryItemViewModel(category)));
            NotifyStateChanged();
        }, "Не удалось загрузить категории");
    }

    public Task<bool> CreateAsync(string name)
    {
        return RunSaveAsync(async () =>
        {
            var created = await apiService.Categories.CreateAsync(new CreateCategoryDto { Name = name.Trim() });
            Categories.Add(new CategoryItemViewModel(created));
            SortCategories();
            Message = "Категория создана.";
        }, "Не удалось изменить категории");
    }

    public Task<bool> RenameAsync(CategoryItemViewModel category, string name)
    {
        return RunSaveAsync(async () =>
        {
            await apiService.Categories.UpdateAsync(category.Id, new UpdateCategoryDto { Name = name.Trim() });
            category.Name = name.Trim();
            SortCategories();
            Message = "Категория переименована.";
        }, "Не удалось изменить категории");
    }

    public Task<bool> DeleteAsync(CategoryItemViewModel category)
    {
        return RunSaveAsync(async () =>
        {
            await apiService.Categories.DeleteAsync(category.Id);
            Categories.Remove(category);
            Message = "Категория удалена.";
            NotifyStateChanged();
        }, "Не удалось изменить категории");
    }

    protected override void OnLoadingStateChanged()
    {
        NotifyStateChanged();
    }

    private void SortCategories()
    {
        Categories.ReplaceWith(Categories.OrderBy(category => category.Name));
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(HasNoCategories));
    }
}

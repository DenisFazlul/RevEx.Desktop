using System.Threading.Tasks;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Categories;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Catalog;

public sealed class ClassificationAdminTabViewModel : WorkspaceTabViewModel
{
    public ClassificationAdminTabViewModel(IRevExApiService apiService)
        : base("Категории и теги", true, "classification")
    {
        Categories = new CategoryAdminTabViewModel(apiService);
        Tags = new TagAdminTabViewModel(apiService);
    }

    public CategoryAdminTabViewModel Categories { get; }

    public TagAdminTabViewModel Tags { get; }

    public override async Task ActivateAsync()
    {
        await Task.WhenAll(Categories.ActivateSafelyAsync(), Tags.ActivateSafelyAsync());
    }

    public override void Dispose()
    {
        Categories.Dispose();
        Tags.Dispose();
        base.Dispose();
    }
}

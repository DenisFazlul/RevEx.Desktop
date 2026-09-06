using System.Collections.Generic;
using RevEx.Desktop.ViewModels.Catalog;

namespace RevEx.Desktop.Navigation;

public sealed class MainMenuProvider : IMainMenuProvider
{
    public IReadOnlyList<WorkspaceTabDescriptor> Items { get; } =
    [
        WorkspaceTabDescriptor.Create<CatalogTabViewModel>("Каталог")
    ];
}

using System;
using System.Collections.Generic;
using System.Linq;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.MainMenu;
using RevEx.Desktop.ViewModels.Settings;

namespace RevEx.Desktop.Navigation;

public sealed class MainMenuProvider : IMainMenuProvider
{
    private readonly IReadOnlyList<WorkspaceTabDescriptor> _items =
    [
        WorkspaceTabDescriptor.Create<CatalogTabViewModel>("Каталог"),
        WorkspaceTabDescriptor.Create<SettingsTabViewModel>("SettingsView")
    ];

    public MainMenuViewModel Create(Action<WorkspaceTabRequest> openTab)
    {
        var links = _items.Select(item => new BtnLink(
            item.Title,
            new WorkspaceTabRequest(item.TabType),
            openTab));

        return new MainMenuViewModel("Г", false, links);
    }
}

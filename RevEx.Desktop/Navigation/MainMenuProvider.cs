using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Services;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.ViewModels.MainMenu;
using RevEx.Desktop.ViewModels.Settings;
using RevEx.Desktop.ViewModels.Tags;

namespace RevEx.Desktop.Navigation;

public sealed class MainMenuProvider : IMainMenuProvider
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IApplicationShutdownService _applicationShutdownService;

    private readonly IReadOnlyList<WorkspaceTabDescriptor> _items =
    [
        WorkspaceTabDescriptor.Create<CatalogTabViewModel>("Каталог", "⌕"),
        WorkspaceTabDescriptor.Create<ContentEditorTabViewModel>("Добавить контент", "+"),
        WorkspaceTabDescriptor.Create<TagAdminTabViewModel>("Теги", "#"),
        WorkspaceTabDescriptor.Create<SettingsTabViewModel>("Настройки", "⚙")
    ];

    public MainMenuProvider(
        IAuthenticationService authenticationService,
        IApplicationShutdownService applicationShutdownService)
    {
        _authenticationService = authenticationService;
        _applicationShutdownService = applicationShutdownService;
    }

    public MainMenuViewModel Create(Action<WorkspaceTabRequest> openTab)
    {
        var links = _items.Select(item => new BtnLink(
            item.Title,
            item.Symbol,
            new WorkspaceTabRequest(item.TabType),
            openTab));

        return new MainMenuViewModel(links, LogoutAsync);
    }

    private async Task LogoutAsync()
    {
        try
        {
            await _authenticationService.LogoutAsync();
        }
        finally
        {
            _applicationShutdownService.Shutdown();
        }
    }
}

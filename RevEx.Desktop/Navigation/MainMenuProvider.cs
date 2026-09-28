using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Services;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Categories;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.ViewModels.MainMenu;
using RevEx.Desktop.ViewModels.Settings;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Desktop.ViewModels.Loading;
using RevEx.Desktop.Core.Services;

namespace RevEx.Desktop.Navigation;

public sealed class MainMenuProvider : IMainMenuProvider
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IApplicationShutdownService _applicationShutdownService;

    public MainMenuProvider(
        IAuthenticationService authenticationService,
        IApplicationShutdownService applicationShutdownService)
    {
        _authenticationService = authenticationService;
        _applicationShutdownService = applicationShutdownService;
    }

    public MainMenuViewModel Create(Action<WorkspaceTabRequest> openTab)
    {
        var items = new List<WorkspaceTabDescriptor>
        {
            WorkspaceTabDescriptor.Create<CatalogTabViewModel>("Каталог", "⌕")
        };
        if (CanEditContent())
        {
            items.Add(WorkspaceTabDescriptor.Create<ContentEditorTabViewModel>("Добавить контент", "+"));
            items.Add(WorkspaceTabDescriptor.Create<LoadingQueueTabViewModel>("Загрузки", "⇩"));
        }
        if (_authenticationService.IsInRole(RevExRoles.Administrator))
        {
            items.Add(WorkspaceTabDescriptor.Create<CategoryAdminTabViewModel>("Категории", "▦"));
            items.Add(WorkspaceTabDescriptor.Create<TagAdminTabViewModel>("Группы тегов", "#"));
        }
        items.Add(WorkspaceTabDescriptor.Create<SettingsTabViewModel>("Настройки", "⚙"));

        var links = items.Select(item => new BtnLink(
            item.Title,
            item.Symbol,
            new WorkspaceTabRequest(item.TabType),
            openTab));

        return new MainMenuViewModel(links, LogoutAsync);
    }

    private bool CanEditContent() =>
        _authenticationService.IsInRole(RevExRoles.Moderator) ||
        _authenticationService.IsInRole(RevExRoles.Administrator);

    private async Task LogoutAsync()
    {
        try
        {
            await _authenticationService.LogoutAsync();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Не удалось отозвать OIDC-сессию при выходе: {exception}");
        }
        finally
        {
            _applicationShutdownService.Shutdown();
        }
    }
}

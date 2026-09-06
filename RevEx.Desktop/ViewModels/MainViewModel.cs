using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.ViewModels.MainMenu;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly MainMenuViewModel _mainMenuViewModel;
    private readonly IWorkspaceTabFactory _tabFactory;
    private readonly WorkspaceTabContext _tabContext;

    [ObservableProperty]
    private WorkspaceTabViewModel _selectedTab;

    public ObservableCollection<WorkspaceTabViewModel> Tabs { get; } = [];

    public MainViewModel(
        IWorkspaceTabFactory tabFactory,
        IMainMenuProvider mainMenuProvider)
    {
        _tabFactory = tabFactory;
        _tabContext = new WorkspaceTabContext(OpenContent);

        var menuLinks = mainMenuProvider.Items
            .Select(item => new BtnLink(item.Title, item.TabType, OpenTab));

        _mainMenuViewModel = new MainMenuViewModel("Г", false, menuLinks);
        Tabs.Add(_mainMenuViewModel);
        _selectedTab = _mainMenuViewModel;
        
       // _catalogTab = new CatalogTabViewModel(revExApiService, OpenContent);
       // Tabs.Add(_catalogTab);
        //_selectedTab = _catalogTab;
    }

    public Task LoadAsync() =>
        Tabs.OfType<CatalogTabViewModel>().FirstOrDefault()?.LoadAsync()
        ?? Task.CompletedTask;

    public void CloseTab(WorkspaceTabViewModel tab)
    {
        if (!tab.CanClose)
            return;

        var tabIndex = Tabs.IndexOf(tab);
        if (tabIndex < 0)
            return;

        Tabs.RemoveAt(tabIndex);
        if (ReferenceEquals(SelectedTab, tab))
            SelectedTab = Tabs[Math.Min(tabIndex, Tabs.Count - 1)];
    }

    private void OpenContent(ContentItemViewModel content)
    {
         
        var tab = Tabs.OfType<ContentDetailsViewModel>().FirstOrDefault(item => item.Id == content.Id);
        if (tab is null)
        {
            tab = new ContentDetailsViewModel(content);
            Tabs.Add(tab);
        }

        SelectedTab = tab;
    }
    private void OpenTab(Type tabType)
    {
        var tab = Tabs.FirstOrDefault(item => item.GetType() == tabType);
        if (tab is null)
        {
            tab = _tabFactory.Create(tabType, _tabContext);
            Tabs.Add(tab);
        }

        SelectedTab = tab;

        if (tab is CatalogTabViewModel catalog && catalog.Categories.Count == 0)
            _ = catalog.LoadAsync();
    }
    
}

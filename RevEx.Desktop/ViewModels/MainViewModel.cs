using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
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
        _tabContext = new WorkspaceTabContext(OpenTab);

        var mainMenu = mainMenuProvider.Create(OpenTab);
        Tabs.Add(mainMenu);
        _selectedTab = mainMenu;
    }

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

    private void OpenTab(WorkspaceTabRequest request)
    {
        var tab = Tabs.FirstOrDefault(item =>
            item.GetType() == request.TabType && Equals(item.Key, request.Key));

        if (tab is null)
        {
            tab = _tabFactory.Create(request.TabType, _tabContext, request.Parameter);
            Tabs.Add(tab);
        }

        SelectedTab = tab;
        _ = tab.ActivateAsync();
    }
}

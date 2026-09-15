using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.MainMenu;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IWorkspaceTabFactory _tabFactory;
    private readonly WorkspaceTabContext _tabContext;

    [ObservableProperty]
    private WorkspaceTabViewModel? _selectedTab;

    [ObservableProperty]
    private bool _hasTabs;

    public ObservableCollection<WorkspaceTabViewModel> Tabs { get; } = [];
    public MainMenuViewModel MainMenu { get; }

    public MainViewModel(
        IWorkspaceTabFactory tabFactory,
        IMainMenuProvider mainMenuProvider)
    {
        _tabFactory = tabFactory;
        _tabContext = new WorkspaceTabContext(OpenTab);

        MainMenu = mainMenuProvider.Create(OpenTab);
        OpenTab(new WorkspaceTabRequest(typeof(CatalogTabViewModel)));
    }

    public void CloseTab(WorkspaceTabViewModel tab)
    {
        if (!tab.CanClose)
            return;

        var tabIndex = Tabs.IndexOf(tab);
        if (tabIndex < 0)
            return;

        Tabs.RemoveAt(tabIndex);
        HasTabs = Tabs.Count > 0;
        if (ReferenceEquals(SelectedTab, tab))
            SelectedTab = Tabs.Count == 0 ? null : Tabs[Math.Min(tabIndex, Tabs.Count - 1)];
    }

    private void OpenTab(WorkspaceTabRequest request)
    {
        var tab = Tabs.FirstOrDefault(item =>
            item.GetType() == request.TabType && Equals(item.Key, request.Key));

        if (tab is null)
        {
            tab = _tabFactory.Create(request.TabType, _tabContext, request.Parameter);
            Tabs.Add(tab);
            HasTabs = true;
        }

        SelectedTab = tab;
    }

    partial void OnSelectedTabChanged(WorkspaceTabViewModel? value)
    {
        foreach (var tab in Tabs)
            tab.IsSelected = ReferenceEquals(tab, value);

        if (value is not null)
            _ = value.ActivateAsync();
    }
}

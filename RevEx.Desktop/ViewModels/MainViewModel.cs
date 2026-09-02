using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly CatalogTabViewModel _catalogTab;

    [ObservableProperty]
    private WorkspaceTabViewModel _selectedTab;

    public ObservableCollection<WorkspaceTabViewModel> Tabs { get; } = [];

    public MainViewModel(IRevExApiService revExApiService)
    {
        _catalogTab = new CatalogTabViewModel(revExApiService, OpenContent);
        Tabs.Add(_catalogTab);
        _selectedTab = _catalogTab;
    }

    public Task LoadAsync() => _catalogTab.LoadAsync();

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
}

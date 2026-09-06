using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Navigation;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.MainMenu;

public sealed class BtnLink
{
    public string Name { get; }
    public ICommand OpenCommand { get; }

    public BtnLink(string name, WorkspaceTabRequest request, Action<WorkspaceTabRequest> openTab)
    {
        Name = name;
        OpenCommand = new RelayCommand(() => openTab(request));
    }
}

public sealed class MainMenuViewModel : WorkspaceTabViewModel
{
    public ObservableCollection<BtnLink> BtnLinks { get; }

    public MainMenuViewModel(
        string title,
        bool canClose,
        IEnumerable<BtnLink> links) : base(title, canClose)
    {
        BtnLinks = new ObservableCollection<BtnLink>(links);
    }
}

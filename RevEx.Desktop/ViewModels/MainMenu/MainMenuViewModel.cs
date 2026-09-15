using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Navigation;

namespace RevEx.Desktop.ViewModels.MainMenu;

public sealed class BtnLink
{
    public string Name { get; }
    public string Symbol { get; }
    public ICommand OpenCommand { get; }

    public BtnLink(string name, string symbol, WorkspaceTabRequest request, Action<WorkspaceTabRequest> openTab)
    {
        Name = name;
        Symbol = symbol;
        OpenCommand = new RelayCommand(() => openTab(request));
    }
}

public sealed class MainMenuViewModel : ViewModelBase
{
    public ObservableCollection<BtnLink> BtnLinks { get; }
    public IAsyncRelayCommand LogoutCommand { get; }

    public MainMenuViewModel(IEnumerable<BtnLink> links, Func<Task> logout)
    {
        BtnLinks = new ObservableCollection<BtnLink>(links);
        LogoutCommand = new AsyncRelayCommand(logout);
    }
}

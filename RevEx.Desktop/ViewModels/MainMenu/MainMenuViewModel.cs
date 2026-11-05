using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Navigation;

namespace RevEx.Desktop.ViewModels.MainMenu;

/// <summary>Модель главного меню. Хранит доступные разделы и команду выхода из профиля.</summary>
public sealed class MainMenuViewModel : ViewModelBase
{
    public ObservableCollection<BtnLink> BtnLinks
    {
        get;
    }
    public IAsyncRelayCommand LogoutCommand
    {
        get;
    }

    public MainMenuViewModel(
        IEnumerable<BtnLink> links,
        Func<Task> logout)
    {
        BtnLinks = new ObservableCollection<BtnLink>(links);
        LogoutCommand = new AsyncRelayCommand(logout);
    }

}

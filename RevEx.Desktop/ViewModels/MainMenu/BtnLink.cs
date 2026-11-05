using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Navigation;

namespace RevEx.Desktop.ViewModels.MainMenu;

/// <summary>Пункт главного меню: подпись, значок и команда открытия вкладки.</summary>
public sealed class BtnLink
{
    public string Name
    {
        get;
    }
    public string Symbol
    {
        get;
    }
    public ICommand OpenCommand
    {
        get;
    }

    public BtnLink(string name, string symbol, WorkspaceTabRequest request, Action<WorkspaceTabRequest> openTab)
    {
        Name = name;
        Symbol = symbol;
        OpenCommand = new RelayCommand(() => openTab(request));
    }
}

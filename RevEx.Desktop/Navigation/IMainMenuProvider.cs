using System;
using RevEx.Desktop.ViewModels.MainMenu;

namespace RevEx.Desktop.Navigation;

public interface IMainMenuProvider
{
    MainMenuViewModel Create(Action<WorkspaceTabRequest> openTab);
}

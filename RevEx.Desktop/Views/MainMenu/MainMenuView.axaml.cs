using Avalonia.Controls;
using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.Views.Contents;

namespace RevEx.Desktop.Views.MainMenu;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();
    }

    private async void OnOpenByIdClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (TopLevel.GetTopLevel(this) is not Window { DataContext: MainViewModel main } owner)
        {
            return;
        }

        var window = new OpenContentByIdWindow(main.CreateOpenContentByIdDialog());
        await window.ShowDialog(owner);
    }
}

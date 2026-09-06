using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RevEx.Desktop.Views.MainMenu;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();
    }

    

    private void Test_OnClick(object? sender, RoutedEventArgs e)
    {
         var context=DataContext;
         Console.Write("test");
    }
}
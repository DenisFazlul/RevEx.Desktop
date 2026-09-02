using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.ViewModels.Contents;

namespace RevEx.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
        Opened += async (_, _) => await viewModel.LoadAsync();
    }

    private void OnContentDoubleTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (DataContext is MainViewModel mainViewModel &&
            sender is Control { DataContext: ContentItemViewModel content })
        {
            mainViewModel.OpenContent(content);
        }
    }

    private void OnCloseTabClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is MainViewModel mainViewModel &&
            sender is Control { DataContext: ContentDetailsViewModel tab })
        {
            mainViewModel.CloseTab(tab);
            eventArgs.Handled = true;
        }
    }
}

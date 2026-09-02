using Avalonia.Controls;
using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.ViewModels.Tabs;

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

    private void OnCloseTabClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is MainViewModel mainViewModel &&
            sender is Control { DataContext: WorkspaceTabViewModel tab })
        {
            mainViewModel.CloseTab(tab);
            eventArgs.Handled = true;
        }
    }
}

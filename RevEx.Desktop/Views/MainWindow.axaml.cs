using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.Views;

public partial class MainWindow : Window
{
    private const double ExpandedSidebarWidth = 264;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
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

    private void OnSelectTabClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is MainViewModel mainViewModel &&
            sender is Control { DataContext: WorkspaceTabViewModel tab })
        {
            mainViewModel.SelectedTab = tab;
            eventArgs.Handled = true;
        }
    }

    private void OnHideSidebarClick(object? sender, RoutedEventArgs eventArgs)
    {
        SetSidebarVisibility(false);
        eventArgs.Handled = true;
    }

    private void OnShowSidebarClick(object? sender, RoutedEventArgs eventArgs)
    {
        SetSidebarVisibility(true);
        eventArgs.Handled = true;
    }

    private void SetSidebarVisibility(bool isVisible)
    {
        WorkspaceSidebar.IsVisible = isVisible;
        WorkspaceGrid.ColumnDefinitions[0].Width = isVisible
            ? new GridLength(ExpandedSidebarWidth)
            : new GridLength(0);
        ShowSidebarButton.IsVisible = !isVisible;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (DataContext is not MainViewModel mainViewModel)
            return;

        var hasCommandModifier = (eventArgs.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        if (!hasCommandModifier)
            return;

        if (eventArgs.Key == Key.W && mainViewModel.SelectedTab is { } selectedTab)
        {
            mainViewModel.CloseTab(selectedTab);
            eventArgs.Handled = true;
            return;
        }

        if (eventArgs.Key == Key.B)
        {
            SetSidebarVisibility(!WorkspaceSidebar.IsVisible);
            eventArgs.Handled = true;
            return;
        }

        if (eventArgs.Key != Key.Tab || mainViewModel.Tabs.Count < 2)
            return;

        var currentIndex = mainViewModel.SelectedTab is null
            ? -1
            : mainViewModel.Tabs.IndexOf(mainViewModel.SelectedTab);
        var direction = eventArgs.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1;
        var nextIndex = (currentIndex + direction + mainViewModel.Tabs.Count) % mainViewModel.Tabs.Count;
        mainViewModel.SelectedTab = mainViewModel.Tabs[nextIndex];
        eventArgs.Handled = true;
    }
}

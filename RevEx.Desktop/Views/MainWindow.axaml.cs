using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly IAuthenticationService? _authenticationService;
    private bool _authenticationStarted;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(
        MainViewModel viewModel,
        IAuthenticationService authenticationService) : this()
    {
        DataContext = viewModel;
        _authenticationService = authenticationService;
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs eventArgs)
    {
        if (_authenticationStarted || _authenticationService is null)
            return;

        _authenticationStarted = true;

        try
        {
            await _authenticationService.GetAccessTokenAsync();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Keycloak startup authentication failed: {exception}");
            await ShowAuthenticationErrorAsync(exception.Message);
        }
    }

    private async Task ShowAuthenticationErrorAsync(string message)
    {
        var closeButton = new Button
        {
            Content = "Закрыть",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var dialog = new Window
        {
            Title = "Ошибка авторизации",
            Width = 460,
            Height = 190,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 20,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Не удалось авторизоваться в Keycloak.\n{message}",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    closeButton
                }
            }
        };

        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
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

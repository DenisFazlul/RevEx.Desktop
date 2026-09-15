using Avalonia.Controls;
using Avalonia.Interactivity;

namespace RevEx.Desktop.Views;

public partial class AuthenticationErrorWindow : Window
{
    public AuthenticationErrorWindow()
    {
        InitializeComponent();
    }

    public AuthenticationErrorWindow(string message) : this()
    {
        MessageText.Text = $"Авторизация не завершена.\n{message}";
    }

    private void OnCloseClick(object? sender, RoutedEventArgs eventArgs)
    {
        Close();
    }
}

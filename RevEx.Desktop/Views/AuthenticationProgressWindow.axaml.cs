using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Views;

public partial class AuthenticationProgressWindow : Window
{
    private readonly IAuthenticationBrowser? _authenticationBrowser;
    private readonly CancellationTokenSource _cancellation = new();
    private bool _completed;

    public AuthenticationProgressWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
    }

    public AuthenticationProgressWindow(IAuthenticationBrowser authenticationBrowser)
        : this()
    {
        _authenticationBrowser = authenticationBrowser;
    }

    public CancellationToken CancellationToken => _cancellation.Token;

    public void Complete()
    {
        _completed = true;
        Close();
        _cancellation.Dispose();
    }

    private void OnReopenBrowserClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            if (_authenticationBrowser is null)
            {
                throw new InvalidOperationException("Служба открытия браузера недоступна.");
            }

            _authenticationBrowser.Reopen();
            StatusText.Text = "Страница входа открыта повторно. Завершите авторизацию в браузере.";
        }
        catch (Exception exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs eventArgs)
    {
        _cancellation.Cancel();
        Close();
    }

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        if (!_completed)
        {
            _cancellation.Cancel();
        }
    }
}

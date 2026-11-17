using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using RevEx.Desktop.Core.Services;

namespace RevEx.Desktop.Views.Common;

public partial class OperationErrorWindow : Window
{
    private string? _diagnostics;

    public OperationErrorWindow()
    {
        InitializeComponent();
    }

    public OperationErrorWindow(string title, Exception failure) : this()
    {
        Title = title;
        Description.Text = failure switch
        {
            RevExApiException apiError => apiError.Message,
            TaskCanceledException => "Сервер не ответил вовремя. Проверьте, сохранились ли изменения, прежде чем повторить действие.",
            HttpRequestException { StatusCode: null } => "Не удалось связаться с сервером. Проверьте подключение и состояние операции.",
            HttpRequestException => "Сервер не смог выполнить операцию. Проверьте данные или обратитесь в поддержку.",
            _ => "Не удалось обработать файл. Проверьте, что файл доступен для чтения."
        };

        if (failure is RevExApiException { Kind: "unexpected", TraceId: { Length: > 0 } traceId } error)
        {
            Reference.Text = $"Код обращения: {traceId}";
            _diagnostics = $"{title}{Environment.NewLine}{error.Code}{Environment.NewLine}{Reference.Text}";
        }

        Reference.IsVisible = _diagnostics is not null;
        CopyButton.IsVisible = _diagnostics is not null;
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (Clipboard is not null && _diagnostics is not null)
        {
            await Clipboard.SetTextAsync(_diagnostics);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs eventArgs)
    {
        Close();
    }
}

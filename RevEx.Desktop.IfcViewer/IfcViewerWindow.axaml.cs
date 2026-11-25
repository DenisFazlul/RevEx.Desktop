using System;
using System.Threading;
using Avalonia.Controls;

namespace RevEx.Desktop.IfcViewer;

/// <summary>Окно кроссплатформенного просмотра одного IFC-файла.</summary>
public partial class IfcViewerWindow : Window
{
    private readonly string _fileName;
    private readonly byte[] _content;
    private readonly CancellationTokenSource _closingCancellation = new();
    private IfcViewerServer? _server;

    public IfcViewerWindow()
    {
        InitializeComponent();
        _fileName = "model.ifc";
        _content = [];
    }

    public IfcViewerWindow(string fileName, byte[] content)
        : this()
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Имя IFC-файла не задано.", nameof(fileName));
        }

        ArgumentNullException.ThrowIfNull(content);

        _fileName = fileName;
        _content = content;
        Title = $"Просмотр IFC — {fileName}";
        FileNameText.Text = fileName;

        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs eventArgs)
    {
        try
        {
            _server = await IfcViewerServer.StartAsync(
                _fileName,
                _content,
                _closingCancellation.Token);

            StatusText.Text = "Загрузка модели…";
            Viewer.Source = _server.ViewerUri;
        }
        catch (OperationCanceledException) when (_closingCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Не удалось открыть IFC: {exception.Message}";
        }
    }

    private async void OnClosed(object? sender, EventArgs eventArgs)
    {
        Opened -= OnOpened;
        Closed -= OnClosed;
        _closingCancellation.Cancel();

        if (_server is not null)
        {
            await _server.DisposeAsync();
            _server = null;
        }

        _closingCancellation.Dispose();
    }
}

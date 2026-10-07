using System;
using Avalonia.Controls;
using RevEx.Desktop.Core.Domain;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentHistoryDetailsWindow : Window
{
    public ContentHistoryDetailsWindow() : this(new ContentHistoryEventDto(
        0,
        0,
        DateTimeOffset.UtcNow,
        string.Empty,
        string.Empty))
    {
    }

    public ContentHistoryDetailsWindow(ContentHistoryEventDto historyEvent)
    {
        InitializeComponent();
        DataContext = historyEvent;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close();
}

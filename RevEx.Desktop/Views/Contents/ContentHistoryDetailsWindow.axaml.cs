using RevEx.Desktop.ViewModels.Contents;
using System;
using Avalonia.Controls;
using RevEx.Domain.Contracts.ContentHistory;

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
        DataContext = new ContentHistoryEventViewModel(historyEvent);
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close();
}

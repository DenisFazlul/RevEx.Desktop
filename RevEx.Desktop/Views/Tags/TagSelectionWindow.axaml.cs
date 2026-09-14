using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using RevEx.Desktop.Core.Domain;
using RevEx.Desktop.ViewModels.Tags;

namespace RevEx.Desktop.Views.Tags;

public partial class TagSelectionWindow : Window
{
    public TagSelectionWindow() : this([])
    {
    }

    public TagSelectionWindow(IEnumerable<TagDto> tags)
    {
        InitializeComponent();
        DataContext = new TagSelectionWindowViewModel(tags);
    }

    private void OnAddClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => CloseSelectedTag();

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close(null);

    private void OnTagDoubleTapped(object? sender, TappedEventArgs eventArgs) => CloseSelectedTag();

    private void CloseSelectedTag()
    {
        if (DataContext is TagSelectionWindowViewModel { SelectedTag: { } tag })
            Close(tag);
    }
}

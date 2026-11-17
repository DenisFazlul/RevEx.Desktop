using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using RevEx.Domain.Contracts.Tag;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Tags;

public partial class TagSelectionWindow : DialogWindow
{
    public TagSelectionWindow() : this([])
    {
    }

    public TagSelectionWindow(IEnumerable<TagDto> tags)
    {
        InitializeComponent();
        DataContext = new TagSelectionWindowViewModel(tags);
    }

    private void OnTagDoubleTapped(object? sender, TappedEventArgs eventArgs) => CloseSelectedTag();

    private void CloseSelectedTag()
    {
        if (DataContext is TagSelectionWindowViewModel viewModel)
        {
            viewModel.Accept();
        }
    }
}

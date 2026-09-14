using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RevEx.Desktop.ViewModels.Contents;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentEditorTabView : UserControl
{
    private static readonly FilePickerFileType RevitFamilyFileType = new("Revit Family")
    {
        Patterns = ["*.rfa"]
    };

    public ContentEditorTabView() => InitializeComponent();

    private async void OnChooseFileClick(object? sender, RoutedEventArgs eventArgs)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите Revit-семейство",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { RevitFamilyFileType }
        });
        var file = files.FirstOrDefault();
        if (file is null || DataContext is not ContentEditorTabViewModel viewModel)
            return;

        viewModel.SetSelectedFile(file.Path.LocalPath, file.Name);
    }
}

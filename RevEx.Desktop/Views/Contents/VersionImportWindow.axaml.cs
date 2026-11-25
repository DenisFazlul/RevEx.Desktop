using System.Linq;
using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Contents;

public partial class VersionImportWindow : DialogWindow
{
    public VersionImportWindow()
    {
        InitializeComponent();
    }

    public VersionImportWindow(VersionImportViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private async void OnChooseFilesClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not VersionImportViewModel { CanChooseFiles: true } viewModel)
        {
            return;
        }

        var files = await LocalFilePicker.OpenMultipleAsync(
            this,
            "Выберите файлы версии",
            LocalFilePicker.AllFiles);
        if (files.Count > 0)
        {
            viewModel.SetFiles(files.Select(file => file.Path).ToArray());
        }
    }
}

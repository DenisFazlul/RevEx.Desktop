using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Contents;

public partial class FileVersionImportWindow : DialogWindow
{
    public FileVersionImportWindow()
    {
        InitializeComponent();
    }

    public FileVersionImportWindow(FileVersionImportViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    private async void OnChooseFileClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not FileVersionImportViewModel { CanChooseFile: true } viewModel)
        {
            return;
        }

        var file = await LocalFilePicker.OpenAsync(this, "Выберите файл версии", LocalFilePicker.AllFiles);
        if (file is not null)
        {
            viewModel.LoadFile(file.Path);
        }
    }
}

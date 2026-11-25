using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Contents;

public partial class RfaFamilyImportWindow : DialogWindow
{
    public RfaFamilyImportWindow()
    {
        InitializeComponent();
    }

    public RfaFamilyImportWindow(RfaFamilyImportViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private async void OnChooseFileClick(object? sender, RoutedEventArgs eventArgs)
    {
        await ChooseFileAsync();
    }

    private async Task ChooseFileAsync()
    {
        if (DataContext is not RfaFamilyImportViewModel { CanChooseFile: true } viewModel)
        {
            return;
        }

        var files = await LocalFilePicker.OpenMultipleAsync(
            this, "Выберите одно семейство RFA и вспомогательные CSV или IFC", LocalFilePicker.RevitFamilyWithAuxiliaryFiles);
        if (files.Count > 0)
        {
            await viewModel.LoadFilesAsync(files.Select(file => file.Path).ToArray());
        }
    }
}

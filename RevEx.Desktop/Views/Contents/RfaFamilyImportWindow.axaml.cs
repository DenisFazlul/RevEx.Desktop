using System;
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
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs eventArgs)
    {
        await ChooseFileAsync();
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

        var file = await LocalFilePicker.OpenAsync(this, "Выберите семейство Revit", LocalFilePicker.RevitFamily);
        if (file is not null)
        {
            await viewModel.LoadFileAsync(file.Path);
        }
    }
}

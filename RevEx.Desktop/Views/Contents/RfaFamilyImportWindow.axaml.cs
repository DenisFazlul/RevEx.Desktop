using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RevEx.Desktop.ViewModels.Contents;

namespace RevEx.Desktop.Views.Contents;

public partial class RfaFamilyImportWindow : Window
{
    private readonly RfaFamilyImportViewModel? _viewModel;

    public RfaFamilyImportWindow()
    {
        InitializeComponent();
    }

    public RfaFamilyImportWindow(RfaFamilyImportViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        Opened += OnOpened;
        Closing += OnClosing;
        Closed += (_, _) => viewModel.Dispose();
    }

    private async void OnOpened(object? sender, EventArgs eventArgs)
    {
        await ChooseFileAsync();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs eventArgs)
    {
        // The connector keeps accepted work alive; do not delete its working file mid-read.
        eventArgs.Cancel = _viewModel?.IsBusy == true;
    }

    private async void OnChooseFileClick(object? sender, RoutedEventArgs eventArgs)
    {
        await ChooseFileAsync();
    }

    private async Task ChooseFileAsync()
    {
        if (_viewModel is null || !_viewModel.CanChooseFile)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите семейство Revit",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Семейство Revit (*.rfa)") { Patterns = ["*.rfa"] }]
        });
        var file = files.FirstOrDefault();
        if (file?.TryGetLocalPath() is string path)
        {
            await _viewModel.LoadFileAsync(path);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_viewModel is not null && await _viewModel.SaveAsync())
        {
            Close(_viewModel.SavedVersion);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs eventArgs)
    {
        Close();
    }
}

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RevEx.Desktop.ViewModels.Families;

namespace RevEx.Desktop.Views.Families;

public partial class FamilyIntegrationTabView : UserControl
{
    private static readonly FilePickerFileType RevitFamilyFileType = new("Revit Family")
    {
        Patterns = ["*.rfa"]
    };

    public FamilyIntegrationTabView() => InitializeComponent();

    private async void OnChooseFamilyClick(object? sender, RoutedEventArgs eventArgs)
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
        if (file is not null && DataContext is FamilyIntegrationTabViewModel viewModel)
            viewModel.SetSelectedFamily(file.Path.LocalPath);
    }
}

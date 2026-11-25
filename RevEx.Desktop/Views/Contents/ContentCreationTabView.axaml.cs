using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Domain.Contracts.ContentVersion;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentCreationTabView : UserControl
{
    public ContentCreationTabView()
    {
        InitializeComponent();
    }

    private async void OnAddRfaFamilyClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not ContentCreationTabViewModel viewModel ||
            TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var window = new RfaFamilyImportWindow(viewModel.CreateRfaFamilyImport());
        var version = await window.ShowDialog<ContentVersionDto?>(owner);
        if (version is not null && owner.DataContext is MainViewModel mainViewModel)
        {
            foreach (var catalog in mainViewModel.Tabs.OfType<CatalogTabViewModel>())
            {
                await catalog.LoadAsync();
            }
        }
    }
}

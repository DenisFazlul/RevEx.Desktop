using Avalonia.Controls;
using Avalonia.Input;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Contents;

namespace RevEx.Desktop.Views.Catalog;

public partial class CatalogTabView : UserControl
{
    public CatalogTabView() => InitializeComponent();

    private void OnContentDoubleTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (DataContext is CatalogTabViewModel catalog &&
            sender is Control { DataContext: ContentItemViewModel content })
            catalog.OpenContent(content);
    }
}

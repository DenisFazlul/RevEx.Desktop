using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Catalog;

public partial class CatalogSettingsWindow : DialogWindow
{
    public CatalogSettingsWindow()
    {
        InitializeComponent();
    }

    public CatalogSettingsWindow(CatalogTabViewModel catalog) : this()
    {
        DataContext = new CatalogSettingsViewModel(catalog);
    }
}

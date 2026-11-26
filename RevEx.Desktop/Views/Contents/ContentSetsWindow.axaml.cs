using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentSetsWindow : DialogWindow
{
    public ContentSetsWindow()
    {
        InitializeComponent();
    }

    public ContentSetsWindow(ContentSetsViewModel viewModel) : this()
    {
        DataContext = viewModel;
        Opened += async (_, _) => await viewModel.InitializeAsync();
    }
}

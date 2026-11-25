using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Contents;

public partial class OpenContentByIdWindow : DialogWindow
{
    public OpenContentByIdWindow(OpenContentByIdViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Opened += (_, _) => IdentifiersBox.Focus();
    }
}

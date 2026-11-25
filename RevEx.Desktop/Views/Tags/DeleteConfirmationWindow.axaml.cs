using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Tags;

public partial class DeleteConfirmationWindow : DialogWindow
{
    public DeleteConfirmationWindow() : this(string.Empty, true)
    {
    }

    public DeleteConfirmationWindow(string message, bool canDelete = true)
    {
        InitializeComponent();
        DataContext = new DeleteConfirmationViewModel(message, canDelete);
    }
}

using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentSetEditorWindow : DialogWindow
{
    public ContentSetEditorWindow()
    {
        InitializeComponent();
    }

    public ContentSetEditorWindow(ContentSetEditorViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}

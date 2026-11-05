using RevEx.Desktop.ViewModels.Tags;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Tags;

public partial class TagGroupEditorWindow : DialogWindow
{
    public TagGroupEditorWindow() : this(null)
    {
    }

    public TagGroupEditorWindow(string? name)
    {
        InitializeComponent();
        DataContext = new TagGroupEditorViewModel(name);
        Opened += (_, _) => NameBox.Focus();
    }
}

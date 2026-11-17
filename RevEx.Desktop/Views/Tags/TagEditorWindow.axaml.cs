using System.Collections.Generic;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Tags;

public partial class TagEditorWindow : DialogWindow
{
    public TagEditorWindow() : this([], null, null)
    {
    }

    public TagEditorWindow(IEnumerable<TagGroupItemViewModel> groups, string? name, TagGroupItemViewModel? selectedGroup)
    {
        InitializeComponent();
        DataContext = new TagEditorViewModel(groups, name, selectedGroup);
        Opened += (_, _) => NameBox.Focus();
    }
}

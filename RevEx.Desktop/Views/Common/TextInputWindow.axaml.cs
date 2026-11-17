using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Common;

public partial class TextInputWindow : DialogWindow
{
    public TextInputWindow() : this(new TextInputOptions("Ввод", "Введите значение", "Значение", string.Empty, string.Empty))
    {
    }

    public TextInputWindow(TextInputOptions options)
    {
        InitializeComponent();
        DataContext = new TextInputViewModel(options);
        Opened += (_, _) => ValueBox.Focus();
    }
}

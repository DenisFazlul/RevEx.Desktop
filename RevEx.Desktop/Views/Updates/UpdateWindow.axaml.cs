using Avalonia.Controls;
using RevEx.Desktop.ViewModels.Updates;

namespace RevEx.Desktop.Views.Updates;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
        Closing += (_, args) =>
        {
            if (DataContext is not UpdateWindowViewModel viewModel)
                return;

            args.Cancel = viewModel.IsDownloading;
            viewModel.ContinueWithoutUpdateCommand.Execute(null);
        };
    }
}

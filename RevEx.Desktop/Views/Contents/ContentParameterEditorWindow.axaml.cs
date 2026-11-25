using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RevEx.Desktop.ViewModels.Contents;

namespace RevEx.Desktop.Views.Contents;

public partial class ContentParameterEditorWindow : Window
{
    public ContentParameterEditorWindow()
    {
        InitializeComponent();
    }

    public ContentParameterEditorWindow(ContentDetailsViewModel owner, ContentParameterRowViewModel editor)
        : this()
    {
        DataContext = editor;
        Title = editor.IsExisting ? "Изменить параметр" : "Добавить параметр";
        void OnOwnerPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ContentDetailsViewModel.ParameterEditor) && owner.ParameterEditor is null)
            {
                Close();
            }
        }

        owner.PropertyChanged += OnOwnerPropertyChanged;
        Closing += (_, args) =>
        {
            args.Cancel = editor.IsBusy && owner.ParameterEditor is not null;
        };
        Closed += (_, _) =>
        {
            owner.PropertyChanged -= OnOwnerPropertyChanged;
            owner.CancelParameterCommand.Execute(null);
        };
    }

    private void OnCancelClick(object? sender, RoutedEventArgs args)
    {
        Close();
    }
}

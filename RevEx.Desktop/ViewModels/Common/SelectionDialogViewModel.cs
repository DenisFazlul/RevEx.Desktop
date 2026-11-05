using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Общая модель выбора одного элемента. Хранит список, текущий выбор и подтверждение.</summary>
public class SelectionDialogViewModel<TItem> : DialogViewModel<TItem> where TItem : class
{
    private TItem? _selectedItem;

    public SelectionDialogViewModel(IEnumerable<TItem> items, TItem? selectedItem = null)
    {
        Items = items.ToArray();
        _selectedItem = selectedItem;
        AcceptCommand = new RelayCommand(Accept, () => SelectedItem is not null);
    }

    public IReadOnlyList<TItem> Items
    {
        get;
    }
    public bool HasItems => Items.Count > 0;
    public TItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                AcceptCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IRelayCommand AcceptCommand
    {
        get;
    }

    public void Accept()
    {
        if (SelectedItem is not null)
        {
            Complete(SelectedItem);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace RevEx.Desktop.ViewModels.Common;

/// <summary>Общий пункт фильтра: идентификатор, название и признак выбора.</summary>
public partial class FilterItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isSelected;

    public FilterItemViewModel(int id, string name, bool isSelected = false, Avalonia.Media.IBrush? colorBrush = null)
    {
        Id = id;
        ColorBrush = colorBrush;
        _name = name;
        _isSelected = isSelected;
    }

    public Avalonia.Media.IBrush? ColorBrush { get; }

    public int Id
    {
        get;
    }
}

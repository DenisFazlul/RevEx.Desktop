using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Domain.Contracts.Revit;

namespace RevEx.Desktop.ViewModels.Revit;

/// <summary>Метаданные семейства и выбор типоразмера для просмотра его параметров.</summary>
public partial class RevitFamilyReadResultViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Types))]
    [NotifyPropertyChangedFor(nameof(HasFamily))]
    [NotifyPropertyChangedFor(nameof(HasTypes))]
    private RevitFamilyReadResult? _family;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Parameters))]
    [NotifyPropertyChangedFor(nameof(HasParameters))]
    private RevitFamilyReadResultType? _selectedType;

    public IReadOnlyList<RevitFamilyReadResultType> Types => Family?.Types ?? [];

    public IReadOnlyList<RevitFamilyReadResultTypeParameter> Parameters => SelectedType?.Parameters ?? [];

    public bool HasFamily => Family is not null;

    public bool HasTypes => Types.Count > 0;

    public bool HasParameters => Parameters.Count > 0;

    partial void OnFamilyChanged(RevitFamilyReadResult? value)
    {
        SelectedType = value?.Types.FirstOrDefault();
    }
}

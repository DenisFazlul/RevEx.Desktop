using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Domain.Contracts.Revit;

namespace RevEx.Desktop.ViewModels.Revit;

/// <summary>Метаданные семейства и выбор типоразмера для просмотра его параметров.</summary>
public partial class RevitFamilyReadResultViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFamily))]
    [NotifyPropertyChangedFor(nameof(HasTypes))]
    private RevitFamilyReadResult? _family;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Parameters))]
    [NotifyPropertyChangedFor(nameof(HasParameters))]
    private RevitFamilyReadResultType? _selectedType;

    public IReadOnlyList<RevitFamilyReadResultType> Types => Family?.Types ?? [];

    public IReadOnlyList<RevitFamilyReadResultTypeParameter> Parameters => SelectedType?.Parameters ?? [];

    public IReadOnlyList<RevitFamilyParameterGroupViewModel> ParameterGroups { get; private set; } = [];

    public bool HasFamily => Family is not null;

    public bool HasTypes => Types.Count > 0;

    public bool HasParameters => Parameters.Count > 0;

    partial void OnFamilyChanged(RevitFamilyReadResult? value)
    {
        OnPropertyChanged(nameof(Types));
        SelectedType = value?.Types.FirstOrDefault();
    }

    partial void OnSelectedTypeChanged(RevitFamilyReadResultType? value)
    {
        ParameterGroups = Parameters
            .GroupBy(parameter => (
                Id: string.IsNullOrWhiteSpace(parameter.GroupId) ? null : parameter.GroupId,
                Name: string.IsNullOrWhiteSpace(parameter.GroupId) ? parameter.GroupName : null))
            .Select(group => new RevitFamilyParameterGroupViewModel(
                group.Key.Id,
                group.Select(parameter => parameter.GroupName)
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? group.Key.Id ?? "Прочее",
                group.ToArray()))
            .ToArray();
        OnPropertyChanged(nameof(ParameterGroups));
    }
}

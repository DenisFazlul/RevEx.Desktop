using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using RevEx.Domain.Contracts.Revit;

namespace RevEx.Desktop.ViewModels.Revit;

/// <summary>Группа параметров Revit с состоянием раскрытия в просмотре семейства.</summary>
public partial class RevitFamilyParameterGroupViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isExpanded;

    public string? GroupId { get; }

    public string Name { get; }

    public IReadOnlyList<RevitFamilyReadResultTypeParameter> Parameters { get; }

    public RevitFamilyParameterGroupViewModel(
        string? groupId,
        string name,
        IReadOnlyList<RevitFamilyReadResultTypeParameter> parameters)
    {
        GroupId = groupId;
        Name = name;
        Parameters = parameters;
    }
}

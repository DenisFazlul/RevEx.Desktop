using System.Collections.Generic;
using System.Linq;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.Connectors;

namespace RevEx.Desktop.ViewModels.Connectors;

/// <summary>Модель выбора приложения для загрузки. Хранит доступные коннекторы и выбранный экземпляр.</summary>
public sealed class ConnectorSelectionViewModel : SelectionDialogViewModel<ConnectorDescriptor>
{
    public ConnectorSelectionViewModel(IEnumerable<ConnectorDescriptor> connectors)
        : base(connectors.OrderBy(connector => connector.Name))
    {
        SelectedItem = Items.FirstOrDefault();
    }
}

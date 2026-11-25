using System.Collections.Generic;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Desktop.ViewModels.Connectors;
using RevEx.Desktop.Views.Common;

namespace RevEx.Desktop.Views.Connectors;

public partial class ConnectorSelectionWindow : DialogWindow
{
    public ConnectorSelectionWindow() : this([])
    {
    }

    public ConnectorSelectionWindow(IEnumerable<ConnectorDescriptor> connectors)
    {
        InitializeComponent();
        DataContext = new ConnectorSelectionViewModel(connectors);
    }
}

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using RevEx.Connector.Contracts;

namespace RevEx.Desktop.Views.Connectors;

public partial class ConnectorSelectionWindow : Window
{
    public IReadOnlyCollection<ConnectorDescriptor> Connectors { get; }
    public ConnectorDescriptor? SelectedConnector { get; set; }
    public bool HasConnectors => Connectors.Count > 0;
    public bool HasNoConnectors => !HasConnectors;

    public ConnectorSelectionWindow() : this([]) { }

    public ConnectorSelectionWindow(IEnumerable<ConnectorDescriptor> connectors)
    {
        Connectors = connectors.OrderBy(item => item.Name).ToArray();
        SelectedConnector = Connectors.FirstOrDefault();
        InitializeComponent();
        DataContext = this;
    }

    private void OnSelectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        if (ConnectorBox.SelectedItem is ConnectorDescriptor connector)
            Close(connector);
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close(null);
}

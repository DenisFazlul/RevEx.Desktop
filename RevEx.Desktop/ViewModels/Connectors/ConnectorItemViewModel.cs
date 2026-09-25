using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Connector.Contracts;

namespace RevEx.Desktop.ViewModels.Connectors;

public partial class ConnectorItemViewModel(
    ConnectorDescriptor connector,
    IConnectorClient connectorClient,
    Action<Guid> remove) : ViewModelBase
{
    public Guid InstanceId => connector.InstanceId;
    public string Name => connector.Name;
    public string Product => connector.Product.ToString();
    public string ProductVersion => connector.ProductVersion;
    public string Address => connector.BaseAddress.AbsoluteUri;
    public int ProcessId => connector.ProcessId;
    public string ActiveDocument => connector.ActiveDocument ?? "Документ не указан";

    [ObservableProperty]
    private string _status = "Не проверен";

    [ObservableProperty]
    private bool _isAvailable;

    [ObservableProperty]
    private bool _isChecking;

    [RelayCommand]
    public async Task PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsChecking = true;
            var response = await connectorClient.GetHealthAsync(connector, cancellationToken);
            IsAvailable = string.Equals(response.Status, "ready", StringComparison.OrdinalIgnoreCase);
            Status = IsAvailable ? "Доступен" : response.Status;
        }
        catch (OperationCanceledException)
        {
            IsAvailable = false;
            Status = "Проверка отменена";
        }
        catch (HttpRequestException)
        {
            IsAvailable = false;
            Status = "Недоступен";
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private void Remove() => remove(InstanceId);
}

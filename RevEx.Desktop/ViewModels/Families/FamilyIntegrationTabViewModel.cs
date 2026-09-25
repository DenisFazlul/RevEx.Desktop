using System;
using System.IO;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.ViewModels.Tabs;
using RevEx.Connector.Contracts;
using RevEx.Desktop.Connectors;

namespace RevEx.Desktop.ViewModels.Families;

public partial class FamilyIntegrationTabViewModel(
    IConnectorClient connectorClient,
    IConnectorRegistry connectorRegistry)
    : WorkspaceTabViewModel("Revit-семейства", true, "revit-families")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunRequest))]
    private string _familyPath = string.Empty;

    [ObservableProperty]
    private string? _familyName;

    [ObservableProperty]
    private string? _familyCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunRequest))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _message;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunRequest))]
    private ConnectorDescriptor? _selectedConnector;

    public ObservableCollection<ConnectorDescriptor> Connectors { get; } = [];

    public bool HasFamilyInfo => !string.IsNullOrWhiteSpace(FamilyName);
    public bool CanRunRequest =>
        !IsBusy && SelectedConnector is not null && !string.IsNullOrWhiteSpace(FamilyPath);

    public override Task ActivateAsync()
    {
        RefreshConnectors();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void RefreshConnectors()
    {
        var selectedId = SelectedConnector?.InstanceId;
        Connectors.Clear();
        foreach (var connector in connectorRegistry.GetAll())
            Connectors.Add(connector);
        SelectedConnector = Connectors.FirstOrDefault(item => item.InstanceId == selectedId)
            ?? Connectors.FirstOrDefault();
    }

    public void SetSelectedFamily(string path)
    {
        FamilyPath = path;
        ClearResult();
    }

    [RelayCommand]
    private async Task InspectAsync(CancellationToken cancellationToken)
    {
        if (!TryValidatePath())
            return;

        await RunAsync(async () =>
        {
            var response = await connectorClient.InspectAsync(
                SelectedConnector!, FamilyPath.Trim(), cancellationToken);
            SetFamilyInfo(response.Family.Name, response.Family.Category);
            Message = "Информация о семействе получена из Revit.";
        });
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (!TryValidatePath())
            return;

        await RunAsync(async () =>
        {
            var response = await connectorClient.LoadAsync(
                SelectedConnector!, FamilyPath.Trim(), cancellationToken);
            SetFamilyInfo(response.Family.Name, response.Family.Category);
            Message = response.Success
                ? $"Семейство «{response.Family.Name}» загружено в активный проект."
                : "Revit не подтвердил загрузку семейства.";
        });
    }

    partial void OnFamilyPathChanged(string value) => ClearResult();
    partial void OnIsBusyChanged(bool value) => InspectCommand.NotifyCanExecuteChanged();

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            Message = null;
            await action();
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Операция отменена или Revit не ответил вовремя.";
        }
        catch (HttpRequestException exception)
        {
            ErrorMessage = $"Не удалось выполнить запрос к Revit: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryValidatePath()
    {
        ErrorMessage = null;
        Message = null;

        if (string.IsNullOrWhiteSpace(FamilyPath))
        {
            ErrorMessage = "Выберите файл семейства.";
            return false;
        }

        if (SelectedConnector is null)
        {
            ErrorMessage = "Выберите подключённый Connector.";
            return false;
        }

        if (!string.Equals(Path.GetExtension(FamilyPath.Trim()), ".rfa", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Можно выбрать только файл семейства Revit (*.rfa).";
            return false;
        }

        return true;
    }

    private void SetFamilyInfo(string name, string category)
    {
        FamilyName = name;
        FamilyCategory = category;
        OnPropertyChanged(nameof(HasFamilyInfo));
    }

    private void ClearResult()
    {
        FamilyName = null;
        FamilyCategory = null;
        Message = null;
        ErrorMessage = null;
        OnPropertyChanged(nameof(HasFamilyInfo));
    }
}

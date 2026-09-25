using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RevEx.Connector.Contracts;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Connectors;
using RevEx.Desktop.Services;
using RevEx.Configuration;
using RevEx.Desktop.ViewModels.Connectors;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Settings;

public partial class SettingsTabViewModel : WorkspaceTabViewModel
{
    private readonly IAppSettings _settings;
    private readonly IConnectorRegistry _connectorRegistry;
    private readonly IConnectorClient _connectorClient;
    private readonly IRevExUserSettingsStore _userSettingsStore;

    [ObservableProperty]
    private string _apiServerAddress;

    [ObservableProperty]
    private string _connectorRegistrationAddress;

    [ObservableProperty]
    private string? _settingsMessage;

    [ObservableProperty]
    private string? _settingsErrorMessage;

    [ObservableProperty]
    private bool _isLoadingConnectors;

    public ObservableCollection<ConnectorItemViewModel> Connectors { get; } = [];
    public bool HasConnectors => Connectors.Count > 0;
    public bool HasNoConnectors => !HasConnectors && !IsLoadingConnectors;

    public SettingsTabViewModel(
        IAppSettings settings,
        IConnectorRegistry connectorRegistry,
        IConnectorClient connectorClient,
        IRevExUserSettingsStore userSettingsStore,
        string title,
        bool canClose,
        object? key = null) : base(title, canClose, key)
    {
        _settings = settings;
        _connectorRegistry = connectorRegistry;
        _connectorClient = connectorClient;
        _userSettingsStore = userSettingsStore;
        _apiServerAddress = settings.ApiPath;
        _connectorRegistrationAddress = settings.ConnectorRegistrationAddress;
    }

    [RelayCommand]
    private void SaveConnections()
    {
        SettingsMessage = null;
        SettingsErrorMessage = null;

        if (!TryNormalizeAddress(ApiServerAddress, requireLoopback: false, out var apiAddress))
        {
            SettingsErrorMessage = "Укажите корректный HTTP(S)-адрес API-сервера.";
            return;
        }

        if (!TryNormalizeAddress(
                ConnectorRegistrationAddress,
                requireLoopback: true,
                out var connectorAddress))
        {
            SettingsErrorMessage =
                "Адрес регистрации Connector должен быть loopback HTTP-адресом.";
            return;
        }

        try
        {
            _userSettingsStore.Save(new RevExUserSettings
            {
                ApiPath = apiAddress,
                ConnectorRegistrationAddress = connectorAddress
            });
            ApiServerAddress = apiAddress;
            ConnectorRegistrationAddress = connectorAddress;
            _settings.ApiPath = apiAddress;
            _settings.ConnectorRegistrationAddress = connectorAddress;
            SettingsMessage = $"Настройки сохранены в {_userSettingsStore.FilePath}. Они применятся после перезапуска Desktop и коннектеров.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            SettingsErrorMessage = $"Не удалось сохранить пользовательские настройки: {exception.Message}";
        }
    }

    public override Task ActivateAsync() => RefreshConnectorsAsync();

    [RelayCommand]
    private async Task RefreshConnectorsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsLoadingConnectors = true;
            Connectors.Clear();
            foreach (var connector in _connectorRegistry.GetAll())
            {
                Connectors.Add(new ConnectorItemViewModel(
                    connector,
                    _connectorClient,
                    RemoveConnector));
            }

            NotifyConnectorStateChanged();
            await Task.WhenAll(Connectors.Select(item => item.PingAsync(cancellationToken)));
        }
        finally
        {
            IsLoadingConnectors = false;
            NotifyConnectorStateChanged();
        }
    }

    partial void OnIsLoadingConnectorsChanged(bool value) => NotifyConnectorStateChanged();

    private void RemoveConnector(Guid instanceId)
    {
        _connectorRegistry.Remove(instanceId);
        var item = Connectors.FirstOrDefault(connector => connector.InstanceId == instanceId);
        if (item is not null)
            Connectors.Remove(item);
        NotifyConnectorStateChanged();
    }

    private void NotifyConnectorStateChanged()
    {
        OnPropertyChanged(nameof(HasConnectors));
        OnPropertyChanged(nameof(HasNoConnectors));
    }

    private static bool TryNormalizeAddress(
        string value,
        bool requireLoopback,
        out string normalizedAddress)
    {
        normalizedAddress = string.Empty;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var address) ||
            (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps) ||
            (requireLoopback && (address.Scheme != Uri.UriSchemeHttp || !address.IsLoopback)))
        {
            return false;
        }

        normalizedAddress = address.AbsoluteUri.EndsWith('/')
            ? address.AbsoluteUri
            : $"{address.AbsoluteUri}/";
        return true;
    }
}

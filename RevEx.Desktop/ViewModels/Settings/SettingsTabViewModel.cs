using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Settings;

public class SettingsTabViewModel : WorkspaceTabViewModel
{
    private readonly IAppSettings _settings;

    public string ApiPath => _settings.ApiPath;

    public SettingsTabViewModel(IAppSettings settings, string title, bool canClose, object? key = null) : base(
        title, canClose, key)
    {
        _settings = settings;
    }
}

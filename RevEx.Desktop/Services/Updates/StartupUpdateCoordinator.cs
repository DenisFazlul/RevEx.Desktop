using System.Threading.Tasks;
using RevEx.Configuration;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.ViewModels.Updates;
using RevEx.Desktop.Views.Updates;

namespace RevEx.Desktop.Services.Updates;

public sealed class StartupUpdateCoordinator(DesktopUpdateService updates, IAppSettings settings,
    IRevExUserSettingsStore userSettings)
{
    public async Task<bool> RunAsync()
    {
        // IDE builds have no managed installation to replace.
        if (!updates.IsInstalled) return true;
        var viewModel = new StartupUpdateViewModel(updates, settings, userSettings);
        var window = new StartupUpdateWindow { DataContext = viewModel };
        window.Show();
        try
        {
            await viewModel.CheckCommand.ExecuteAsync(null);
            return await viewModel.Decision;
        }
        finally { window.Close(); }
    }
}

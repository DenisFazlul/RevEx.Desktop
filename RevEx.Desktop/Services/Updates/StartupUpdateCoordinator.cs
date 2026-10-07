using System;
using System.Threading.Tasks;
using RevEx.Desktop.ViewModels.Updates;
using RevEx.Desktop.Views.Updates;

namespace RevEx.Desktop.Services.Updates;

public interface IStartupUpdateCoordinator
{
    Task<bool> RunAsync();
}

public sealed class StartupUpdateCoordinator(IAppUpdateService updates) : IStartupUpdateCoordinator
{
    public async Task<bool> RunAsync()
    {
        UpdateWindow? window = null;
        try
        {
            var update = await updates.CheckAsync();
            if (update is null)
                return true;

            var viewModel = new UpdateWindowViewModel(updates, update);
            window = new UpdateWindow { DataContext = viewModel };
            window.Show();

            if (!await viewModel.Decision)
                return true;

            updates.ApplyAndRestart(update);
            return false;
        }
        catch (Exception exception)
        {
            // Update availability must never prevent using the installed version.
            Console.Error.WriteLine($"Desktop update failed: {exception}");
            return true;
        }
        finally
        {
            window?.Close();
        }
    }
}

using System.Threading.Tasks;

namespace RevEx.Desktop.Services;

public interface IApplicationShutdownService
{
    Task SignOutAsync();
}

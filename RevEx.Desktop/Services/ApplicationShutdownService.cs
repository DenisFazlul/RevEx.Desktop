using System;
using System.Threading.Tasks;

namespace RevEx.Desktop.Services;

internal sealed class ApplicationShutdownService(Func<Task> signOut) : IApplicationShutdownService
{
    public Task SignOutAsync()
    {
        return signOut();
    }
}

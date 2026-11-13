using System.Diagnostics;
using RevEx.Desktop.Core.Interfaces;

namespace RevEx.Desktop.Core.Authentication;

public sealed class SystemAuthenticationBrowser : IAuthenticationBrowser
{
    private readonly object _syncRoot = new();
    private Uri? _authorizationUri;

    public bool CanReopen
    {
        get
        {
            lock (_syncRoot)
            {
                return _authorizationUri is not null;
            }
        }
    }

    public void Open(Uri authorizationUri)
    {
        ArgumentNullException.ThrowIfNull(authorizationUri);

        lock (_syncRoot)
        {
            _authorizationUri = authorizationUri;
        }

        StartBrowser(authorizationUri);
    }

    public void Reopen()
    {
        Uri authorizationUri;
        lock (_syncRoot)
        {
            authorizationUri = _authorizationUri
                ?? throw new InvalidOperationException("Адрес авторизации ещё не получен.");
        }

        StartBrowser(authorizationUri);
    }

    private static void StartBrowser(Uri authorizationUri)
    {
        var startInfo = new ProcessStartInfo(authorizationUri.AbsoluteUri)
        {
            UseShellExecute = true
        };
        Process.Start(startInfo);
    }
}

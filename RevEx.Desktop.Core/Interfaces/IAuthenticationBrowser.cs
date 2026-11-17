namespace RevEx.Desktop.Core.Interfaces;

public interface IAuthenticationBrowser
{
    bool CanReopen { get; }

    void Open(Uri authorizationUri);

    void Reopen();
}

namespace RevEx.Connector.Contracts;

public enum FileLoadingStatus
{
    Accepted,
    Started,
    Completed
}

public sealed record ContentVersionLoadFile(Guid Id, string Name, string Path);

public sealed record LoadContentVersionRequest(
    IReadOnlyCollection<ContentVersionLoadFile> Files,
    Uri StatusCallbackAddress);

public sealed record LoadContentVersionResponse(bool Success, int LoadedFileCount);

public sealed record FileLoadingStatusUpdate(Guid Id, FileLoadingStatus Status);

public sealed record RevitApiErrorResponse(RevitApiError Error);

public sealed record RevitApiError(string Code, string Message);

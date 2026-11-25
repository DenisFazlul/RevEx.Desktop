using System.Net;

namespace RevEx.Desktop.Core.Services;

public sealed class RevExApiException(
    HttpStatusCode status,
    string kind,
    string? code,
    string? traceId,
    string detail) : HttpRequestException(detail, null, status)
{
    public string Kind { get; } = kind;
    public string? Code { get; } = code;
    public string? TraceId { get; } = traceId;
}

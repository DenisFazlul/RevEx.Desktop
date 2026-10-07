namespace RevEx.Desktop.Core.Domain;

public sealed record ContentHistoryEventDto(
    long Id,
    int ContentId,
    DateTimeOffset OccurredAt,
    string Description,
    string Author)
{
    public string OccurredAtText => OccurredAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
}

public sealed record CreateContentHistoryEventDto(string Description);

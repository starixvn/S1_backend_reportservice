namespace S1.Alpha.Api.ViewModels;

public record GetPlayerWinLossByRefIdRequest
{
    public DateTimeOffset From { get; init; }

    public DateTimeOffset To { get; init; }

    public string? ReferenceIds { get; init; }
}

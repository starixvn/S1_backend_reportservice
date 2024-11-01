namespace S1.Alpha.Api.ViewModels;

public record SsoSignInRequest
{
    public string? ReferenceId { get; init; }

    public string? BrowserUserAgent { get; init; }

    public string? UserIp { get; init; }
}

namespace S1.Alpha.Api.ViewModels;

public class PlayerWinLossModel
{
    public string? GameProviderCode { get; init; }

    public decimal PlayerWinLoss { get; init; }

    public string? WinLossDate { get; set; }

    public int TypeId { get; init; }

    public string? ReferenceId { get; init; }
}

namespace ReportService.Core.Models.MpaWinLoss;

public class SummariesMpaModel
{
    public decimal TurnOver { get; set; }
    public decimal ValidTurnOver { get; set; }
    public decimal Ggr { get; set; }
    public decimal PlayerWinLoss { get; set; }
    public decimal BaPlayerWinLoss { get; set; }
    public decimal ProgressiveWin { get; set; }
    public decimal ProgressiveBet { get; set; }
    public decimal Commission { get; set; }
    public decimal BaCommission { get; set; }
    public int BetCount { get; set; }
    public int UniquePlayers { get; set; }
    public int DepositCount { get; set; }
    public decimal TotalDepositAmount { get; set; }
    public int WithdrawalCount { get; set; }
    public decimal TotalWithdrawalAmount { get; set; }
    public decimal TotalBonus { get; set; }
}
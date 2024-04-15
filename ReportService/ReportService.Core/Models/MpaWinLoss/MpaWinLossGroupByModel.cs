using Newtonsoft.Json;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByModel
{
    public void ExchangeMpaWinLossGroupBySummary(decimal rate)
    {
        TurnOver *= rate;
        ValidTurnOver *= rate;
        Ggr *= rate;
        PlayerWinLoss *= rate;
        BaPlayerWinLoss *= rate;
        ProgressiveWin *= rate;
        ProgressiveBet *= rate;
        Commission *= rate;
        BaCommission *= rate;
        AvgBetAmount *= rate;
        Margin *= rate;
        TotalDepositAmount *= rate;
        TotalWithdrawalAmount *= rate;
        TotalBonus *= rate;
    }

    [JsonIgnore]
    public DateTime Date { get; set; }

    public int CurrencyId { get; set; }

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

    public decimal AvgBetAmount { get; set; }

    public int UniquePlayers { get; set; }

    public decimal Margin { get; set; }

    public int DepositCount { get; set; }

    public decimal TotalDepositAmount { get; set; }

    public int WithdrawalCount { get; set; }

    public decimal TotalWithdrawalAmount { get; set; }

    public decimal TotalBonus { get; set; }

    public int CustId { get; set; }

    public string Key { get; set; }
    public string ChildKey { get; set; }

    public string TpUserName { get; set; }
    public string UserName { get; set; }

    public string TpGameType { get; set; }
    public string TpCategory { get; set; }

    public string Brand { get; set; }
}
using ReportService.Core.Models.Enums;

namespace ReportService.Core.Models.Interface;

public interface IMpaWinLossGroupBySummary
{
    public CurrencyEnum CurrencyId { get; set; }

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
}

public class MpaWinLossGroupBySummary : IMpaWinLossGroupBySummary
{
    public MpaWinLossGroupBySummary()
    {
    }
    
    public MpaWinLossGroupBySummary(IMpaWinLossGroupBySummary data)
    {
        CurrencyId = data.CurrencyId;
        TurnOver = data.TurnOver;
        ValidTurnOver = data.ValidTurnOver;
        Ggr = data.Ggr;
        PlayerWinLoss = data.PlayerWinLoss;
        BaPlayerWinLoss = data.BaPlayerWinLoss;
        ProgressiveWin = data.ProgressiveWin;
        ProgressiveBet = data.ProgressiveBet;
        Commission = data.Commission;
        BaCommission = data.BaCommission;
        BetCount = data.BetCount;
        AvgBetAmount = data.AvgBetAmount;
        UniquePlayers = data.UniquePlayers;
        Margin = data.Margin;
        DepositCount = data.DepositCount;
        TotalDepositAmount = data.TotalDepositAmount;
        WithdrawalCount = data.WithdrawalCount;
        TotalWithdrawalAmount = data.TotalWithdrawalAmount;
        TotalBonus = data.TotalBonus;
    }
    
    public MpaWinLossGroupBySummary(IMpaWinLossGroupBySummary data, decimal rate)
    {
        CurrencyId = data.CurrencyId;
        TurnOver = data.TurnOver * rate;
        ValidTurnOver = data.ValidTurnOver * rate;
        Ggr = data.Ggr * rate;
        PlayerWinLoss = data.PlayerWinLoss * rate;
        BaPlayerWinLoss = data.BaPlayerWinLoss * rate;
        ProgressiveWin = data.ProgressiveWin * rate;
        ProgressiveBet = data.ProgressiveBet * rate;
        Commission = data.Commission * rate;
        BaCommission = data.BaCommission * rate;
        BetCount = data.BetCount;
        AvgBetAmount = data.AvgBetAmount * rate;
        UniquePlayers = data.UniquePlayers;
        Margin = data.Margin * rate;
        DepositCount = data.DepositCount;
        TotalDepositAmount = data.TotalDepositAmount * rate;
        WithdrawalCount = data.WithdrawalCount;
        TotalWithdrawalAmount = data.TotalWithdrawalAmount * rate;
        TotalBonus = data.TotalBonus * rate;
    }
    
    public CurrencyEnum CurrencyId { get; set; }

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
}
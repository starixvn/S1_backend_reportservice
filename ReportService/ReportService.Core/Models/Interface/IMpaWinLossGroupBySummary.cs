namespace ReportService.Core.Models.Interface;

public interface IMpaWinLossGroupBySummary
{
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
}

public class MpaWinLossGroupBySummary //: IMpaWinLossGroupBySummary
{
    public MpaWinLossGroupBySummary()
    {
    }
    
    public MpaWinLossGroupBySummary(MpaWinLossGroupBySummary data)
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
    
    public MpaWinLossGroupBySummary ExchangeMpaWinLossGroupBySummary(decimal rate)
    {
        return new MpaWinLossGroupBySummary
        {
            CurrencyId = CurrencyId,
            TurnOver = TurnOver * rate,
            ValidTurnOver = ValidTurnOver * rate,
            Ggr = Ggr * rate,
            PlayerWinLoss = PlayerWinLoss * rate,
            BaPlayerWinLoss = BaPlayerWinLoss * rate,
            ProgressiveWin = ProgressiveWin * rate,
            ProgressiveBet = ProgressiveBet * rate,
            Commission = Commission * rate,
            BaCommission = BaCommission * rate,
            BetCount = BetCount,
            AvgBetAmount = AvgBetAmount * rate,
            UniquePlayers = UniquePlayers,
            Margin = Margin * rate,
            DepositCount = DepositCount,
            TotalDepositAmount = TotalDepositAmount * rate,
            WithdrawalCount = WithdrawalCount,
            TotalWithdrawalAmount = TotalWithdrawalAmount * rate,
            TotalBonus = TotalBonus * rate
        };
    }
    
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
}
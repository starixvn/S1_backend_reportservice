using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLoseResponse<T>
{
    public IEnumerable<T> Result { get; set; }
    
    public IEnumerable<T> ExchangedResult { get; set; }

    public T Total { get; set; }
}

public class MpaWinLossGroupByResponse : MpaWinLossSimpleGroupByResponse
{
    public MpaWinLossGroupByResponse()
    {
    }

    public MpaWinLossGroupByResponse(IMpaWinLossGroupBySummary data, string key)
        : base(data, key)
    {
        DepositCount = data.DepositCount;
        TotalDepositAmount = data.TotalDepositAmount;
        WithdrawalCount = data.WithdrawalCount;
        TotalWithdrawalAmount = data.TotalWithdrawalAmount;
        TotalBonus = data.TotalBonus;
    }

    public int DepositCount { get; set; }

    public decimal TotalDepositAmount { get; set; }

    public int WithdrawalCount { get; set; }

    public decimal TotalWithdrawalAmount { get; set; }

    public decimal TotalBonus { get; set; }
}

public class MpaWinLossSimpleGroupByResponse
{
    public MpaWinLossSimpleGroupByResponse()
    {
    }

    public MpaWinLossSimpleGroupByResponse(IMpaWinLossGroupBySummary data, string key)
    {
        Key = key;
        Currency = data.CurrencyId.ToString();
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
    }
    
    public string Key { get; set; }

    public string Currency { get; set; }

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
}
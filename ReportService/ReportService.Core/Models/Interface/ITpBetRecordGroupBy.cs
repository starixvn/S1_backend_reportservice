using ReportService.Core.Models.Enums;

namespace ReportService.Core.Models.Interface;

public interface ITpBetRecordGroupBy
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
}

public class TpBetRecordGroupBy : ITpBetRecordGroupBy
{
    public TpBetRecordGroupBy()
    {
    }
    
    public TpBetRecordGroupBy(ITpBetRecordGroupBy data)
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
}
using ReportService.Core.Models.Enums;

namespace ReportService.Core.Models;

public class TpBetRecordByTpGameType
{
    public string TpGameTypeName { get; set; }

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

public class TpBetRecordByTpGameTypeWrapper
{
    public IEnumerable<TpBetRecordByTpGameType> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
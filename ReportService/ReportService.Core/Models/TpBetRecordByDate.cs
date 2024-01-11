using ReportService.Core.Models.Enums;

namespace ReportService.Core.Models;

public class TpBetRecordByDateWrapper
{
    public IEnumerable<MpaWinLossByDateDto> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}

public class MpaWinLossByDateDto
{
    public DateTime Date { get; set; }

    public CurrencyEnum CurrencyId { get; set; }

    public decimal TurnOver { get; set; }

    public decimal ValidTurnOver { get; set; }

    public decimal GGR { get; set; }

    public decimal PlayerWinLoss { get; set; }
    public decimal BaPlayerWinLoss { get; set; }

    public decimal ProgressiveWin { get; set; }

    public decimal ProgressiveBet { get; set; }

    public decimal Commission { get; set; }
    public decimal BACommission { get; set; }

    public int BetCount { get; set; }

    public decimal AvgBetAmount { get; set; }

    public int UniquePlayers { get; set; }

    public decimal Margin { get; set; }

    public int DepositCount { get; set; }

    public decimal TotalDepositAmount { get; set; }

    public int WithDrawCount { get; set; }

    public decimal TotalWithDrawAmount { get; set; }

    public decimal TotalBonus { get; set; }
    public int SiteId { get; set; } 
    public int CustId {get; set; }
}
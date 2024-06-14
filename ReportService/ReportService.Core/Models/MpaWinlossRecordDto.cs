using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class MpaWinlossRecordDto : IFilterMpaWinLoss
{
    public DateTime Date { get; set; }
    public int TpId { get; set; }
    public int CategoryId { get; set; }
    public int GameType { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int CustId { get; set; }
    public string UserName { get; set; }
    public string TpUserName { get; set; }
    public byte SettlementType { get; set; }
    public int BonusId { get; set; }
    public byte PromotionType { get; set; }
    public byte TicketStatus { get; set; }
    public decimal Stake { get; set; }
    public decimal ValidBet { get; set; }
    public decimal WinLoss { get; set; }
    public decimal Revenue { get; set; }
    public decimal ProgressiveWin { get; set; }
    public decimal ProgressiveBet { get; set; }
    public int RowCount { get; set; }
    public int IsTest { get; set; }
    public int SummaryType { get; set; }

    public string AffiliateName { get; set; }

    // SABA BA玩家使用
    public decimal BaStake { get; set; }
    public decimal BaValidBet { get; set; }
    public decimal BaWinLoss { get; set; }
    public decimal BaRevenue { get; set; }
    public decimal BaCommision { get; set; }
    public decimal BaProgressiveWin { get; set; }
    public decimal BaProgressiveBet { get; set; }

    public decimal GGR { get; set; }
}


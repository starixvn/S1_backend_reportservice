using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class MpaTpBetRecordDto : IFilterMpaWinLoss
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
    public decimal StakeSummary { get; set; }
    public decimal ValidBetSummary { get; set; }
    public decimal WinLossSummary { get; set; }
    public decimal RevenueSummary { get; set; }
    public decimal ProgressiveWinSummary { get; set; }
    public decimal ProgressiveBetSummary { get; set; }
    public int RowCount { get; set; }
    public int IsTest { get; set; }
    public int SummaryType { get; set; }
    
    public string AffiliateName { get; set; }
    
    // SABA BA玩家使用
    public decimal BaStakeSummary { get; set; }
    public decimal BaValidBetSummary { get; set; }
    public decimal BaWinLossSummary { get; set; }
    public decimal BaRevenueSummary { get; set; }
    public decimal BaProgressiveWinSummary { get; set; }
    public decimal BaProgressiveBetSummary { get; set; }
}


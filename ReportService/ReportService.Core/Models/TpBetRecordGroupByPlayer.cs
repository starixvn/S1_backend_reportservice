using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class MpaWinLossGroupBySummaryPlayer : MpaWinLossGroupBySummary
{
    public MpaWinLossGroupBySummaryPlayer()
    {
    }

    public MpaWinLossGroupBySummaryPlayer(IMpaWinLossGroupBySummary data): base(data)
    {
    }
    
    public string UserName { get; set; }

    public string TpUserName { get; set; }

    public string GameTypeName { get; set; }
    
    public int DepositCount { get; set; }

    public decimal TotalDepositAmount { get; set; }

    public int WithdrawalCount { get; set; }

    public decimal TotalWithdrawalAmount { get; set; }

    public decimal ClosingBalance { get; set; }

    public decimal TotalBonus { get; set; }
    
    public int SiteId { get; set; }
}

public class TpBetRecordByPlayerWrapper
{
    public IEnumerable<MpaWinLossGroupBySummaryPlayer> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
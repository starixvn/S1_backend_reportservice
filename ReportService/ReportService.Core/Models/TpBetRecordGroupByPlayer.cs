using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class TpBetRecordGroupByPlayer : TpBetRecordGroupBy
{
    public TpBetRecordGroupByPlayer()
    {
    }

    public TpBetRecordGroupByPlayer(ITpBetRecordGroupBy data): base(data)
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
    public IEnumerable<TpBetRecordGroupByPlayer> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
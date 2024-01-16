using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class TpBetRecordGroupByAffiliate : TpBetRecordGroupBy
{
    public TpBetRecordGroupByAffiliate()
    {
    }

    public TpBetRecordGroupByAffiliate(ITpBetRecordGroupBy data): base(data)
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
    
    public string AffiliateAccount { get;set; } 
}

public class TpBetRecordByAffiliateWrapper
{
    public IEnumerable<TpBetRecordGroupByAffiliate> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
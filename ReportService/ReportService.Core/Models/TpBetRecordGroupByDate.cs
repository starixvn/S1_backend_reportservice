using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class TpBetRecordByDateWrapper
{
    public IEnumerable<TpBetRecordGroupByDate> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}

public class TpBetRecordGroupByDate : TpBetRecordGroupBy
{
    public TpBetRecordGroupByDate()
    {
    }

    public TpBetRecordGroupByDate(ITpBetRecordGroupBy data): base(data)
    {
    }
    
    public DateTime Date { get; set; }

    public int SiteId { get; set; }

    public int CustId { get; set; }

    public int DepositCount { get; set; }

    public decimal TotalDepositAmount { get; set; }

    public int WithdrawalCount { get; set; }

    public decimal TotalWithdrawalAmount { get; set; }

    public decimal TotalBonus { get; set; }
}
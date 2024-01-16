using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class TpBetRecordGroupByCurrency: TpBetRecordGroupBy
{
    public TpBetRecordGroupByCurrency()
    {
    }

    public TpBetRecordGroupByCurrency(ITpBetRecordGroupBy data): base(data)
    {
    }
    
    public int CustId { get; set; }

    public int DepositCount { get; set; }

    public decimal TotalDepositAmount { get; set; }

    public int WithdrawalCount { get; set; }

    public decimal TotalWithdrawalAmount { get; set; }

    public decimal ClosingBalance { get; set; }

    public decimal TotalBonus { get; set; }
}

public class TpBetRecordByCurrencyWrapper
{
    public IEnumerable<TpBetRecordGroupByCurrency> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
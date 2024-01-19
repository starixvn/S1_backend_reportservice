using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class MpaWinLossGroupByDateWrapper
{
    public IEnumerable<MpaWinLossGroupByDate> Result { get; set; }
    
    public IEnumerable<MpaWinLossGroupByDate> ExchangedResult { get; set; }

    public int UniquePlayers { get; set; }
}

public class MpaWinLossGroupByDate : MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByDate()
    {
    }

    public MpaWinLossGroupByDate(IMpaWinLossGroupBySummary data): base(data)
    {
    }
    
    public MpaWinLossGroupByDate(IMpaWinLossGroupBySummary data, decimal rate): base(data, rate)
    {
    }
    
    public DateTime Date { get; set; }

    public int CustId { get; set; }
}
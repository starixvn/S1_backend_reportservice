using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByDate : MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByDate()
    {
    }

    public MpaWinLossGroupByDate(MpaWinLossGroupBySummary data): base(data)
    {
    }
    
    public DateTime Date { get; set; }

    public int CustId { get; set; }
}
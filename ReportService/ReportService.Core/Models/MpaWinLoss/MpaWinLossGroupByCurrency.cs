using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByCurrency: MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByCurrency()
    {
    }

    public MpaWinLossGroupByCurrency(IMpaWinLossGroupBySummary data): base(data)
    {
    }
    
    public int CustId { get; set; }
    public DateTime Date { get; set; }
}
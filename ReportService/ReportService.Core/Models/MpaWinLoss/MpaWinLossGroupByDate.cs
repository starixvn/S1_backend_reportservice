using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByDate : MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByDate()
    {
    }

    public MpaWinLossGroupByDate(IMpaWinLossGroupBySummary data): base(data)
    {
    }
    
    public DateTime Date { get; set; }

    public int CustId { get; set; }
}
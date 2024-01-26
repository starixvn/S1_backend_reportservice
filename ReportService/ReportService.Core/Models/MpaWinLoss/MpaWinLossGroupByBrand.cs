using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByBrand: MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByBrand()
    {
    }

    public MpaWinLossGroupByBrand(IMpaWinLossGroupBySummary data): base(data)
    {
    }
    
    public int CustId { get; set; }
    public DateTime Date { get; set; }
    
    public int SiteId { get; set; }
}
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByTpCategory : MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByTpCategory()
    {
    }
    
    public MpaWinLossGroupByTpCategory(IMpaWinLossGroupBySummary data)
        : base(data)
    {
    }
    
    public string TpCategoryName { get; set; }
    public DateTime Date { get; set; }
    public int CustId { get; set; }
}

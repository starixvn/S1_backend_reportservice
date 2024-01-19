using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class MpaWinLossGroupBySummaryTpCategory : MpaWinLossGroupBySummary
{
    public MpaWinLossGroupBySummaryTpCategory()
    {
    }
    
    public MpaWinLossGroupBySummaryTpCategory(IMpaWinLossGroupBySummary data, string tpCategoryName)
        : base(data)
    {
        TpCategoryName = tpCategoryName;
    }
    
    public string TpCategoryName { get; set; }
}

public class TpBetRecordByTpCategoryWrapper
{
    public IEnumerable<MpaWinLossGroupBySummaryTpCategory> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
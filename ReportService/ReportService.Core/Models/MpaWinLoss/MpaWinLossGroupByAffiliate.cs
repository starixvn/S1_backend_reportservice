using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByAffiliate : MpaWinLossGroupByPlayer
{
    public MpaWinLossGroupByAffiliate()
    {
    }

    public MpaWinLossGroupByAffiliate(IMpaWinLossGroupBySummary data): base(data)
    {
    }

    public string AffiliateAccount { get;set; } 
}
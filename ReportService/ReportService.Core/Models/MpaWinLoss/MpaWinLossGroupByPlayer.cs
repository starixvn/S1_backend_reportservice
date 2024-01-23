using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByPlayer : MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByPlayer()
    {
    }

    public MpaWinLossGroupByPlayer(IMpaWinLossGroupBySummary data): base(data)
    {
    }
    
    public string UserName { get; set; }

    public string TpUserName { get; set; }

    public string GameTypeName { get; set; }

    public int SiteId { get; set; }
    public DateTime Date { get; set; }
}
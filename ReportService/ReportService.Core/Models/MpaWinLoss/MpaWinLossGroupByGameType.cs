using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByGameType : MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByGameType()
    {
    }
    
    public MpaWinLossGroupByGameType(IMpaWinLossGroupBySummary data)
        : base(data)
    {
    }
    
    public DateTime Date { get; set; }
    
    public string TpGameTypeName { get; set; }
    
    public int CustId { get; set; }
}
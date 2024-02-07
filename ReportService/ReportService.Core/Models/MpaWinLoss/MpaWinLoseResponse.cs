using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLoseResponse
{
    public IEnumerable<MpaWinLossGroupByModel> Result { get; set; }
    
    public IEnumerable<MpaWinLossGroupByModel> ChildResult { get; set; }
    
    public MpaWinLossGroupByModel Total { get; set; }

}

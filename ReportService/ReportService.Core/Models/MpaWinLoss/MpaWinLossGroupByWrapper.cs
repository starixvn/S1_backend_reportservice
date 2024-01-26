namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByWrapper
{
    public IEnumerable<MpaWinLossGroupByModel> Result { get; set; }
    
    public IEnumerable<MpaWinLossGroupByModel> ExchangedResult { get; set; }

    public int UniquePlayers { get; set; }
}
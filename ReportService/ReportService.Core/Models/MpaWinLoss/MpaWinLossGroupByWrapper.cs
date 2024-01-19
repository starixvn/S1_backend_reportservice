namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByWrapper<T>
{
    public IEnumerable<T> Result { get; set; }
    
    public IEnumerable<T> ExchangedResult { get; set; }

    public int UniquePlayers { get; set; }
}
namespace ReportService.Core.Models.MpaWinLoss;

public class GroupedSummary<TKey>
{
    public TKey GroupKey { get; set; }
    public IEnumerable<MpaWinlossRecordDto> Records { get; set; }
    public SummariesModel Summaries { get; set; }
}

public class GroupedSummaryTest<TKey>
{
    public TKey GroupKey { get; set; }
    public IEnumerable<MpaWinLossGroupByModel> Records { get; set; }
    public SummariesMpaModel Summaries { get; set; }
}
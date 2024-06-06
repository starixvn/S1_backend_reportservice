namespace ReportService.Core.Models.MpaWinLoss;

public class SummariesModel
{
    public decimal TotalStakeSummary { get; set; }
    public decimal TotalValidBetSummary { get; set; }
    public decimal TotalWinLossSummary { get; set; }
    public decimal TotalProgressiveWinSummary { get; set; }
    public decimal TotalProgressiveBetSummary { get; set; }
    public int TotalRowCount { get; set; }
    public decimal TotalBaCommission { get; set; }
    public decimal TotalBaWinLossSummary { get; set; }
    public decimal GGR { get; set; }
    public decimal TotalCommission { get; set; }
}
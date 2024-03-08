namespace ReportService.Core.Models.Interface;

public interface IFilterMpaWinLoss
{
    public string AffiliateName { get; set; }
    
    public int IsTest { get; set; }
}
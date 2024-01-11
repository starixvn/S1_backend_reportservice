namespace ReportService.Core.Models.Interface;

public interface IMultiSiteApiRequest
{
    public IEnumerable<int> SiteIds { get; set; }
    public IEnumerable<int> CurrencyIds { get; set; }
}
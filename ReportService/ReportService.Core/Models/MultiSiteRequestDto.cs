namespace ReportService.Core.Models;

public class MultiSiteRequestDto
{
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
}

public class MultiSiteAndCurrencyIds
{
    public int SiteId { get; set; }
    public IEnumerable<int> CurrencyIds { get; set; }
}
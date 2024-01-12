using ReportService.Core.Models.Enums;

namespace ReportService.Core.Models.Interface;

public interface IMultiSiteApiRequest
{
    public IEnumerable<int> SiteIds { get; set; }
    public IEnumerable<CurrencyEnum> CurrencyIds { get; set; }
}
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using Newtonsoft.Json;
using ReportService.Core.Extensions;
using ReportService.Core.Library;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MarketingReport;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Extensions;
using ReportService.Service.DAL;

namespace ReportService.Service.BLL;

public class MarketingBLL
{
    private readonly MarketingService _marketingService;

    public MarketingBLL(IServiceProvider provider)
    {
        _marketingService = provider.GetRequiredService<MarketingService>();
    }

    public async Task<MarketingReportResponse> GetMarketingMarketingDailyReport(MarketingReportRequest request)
    {
        return await _marketingService.GetMarketingMarketingDailyReport(request);
    }


}
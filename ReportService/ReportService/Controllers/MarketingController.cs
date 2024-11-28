using Microsoft.AspNetCore.Mvc;

using ReportService.Core.Models.MarketingReport;
using ReportService.Models;
using ReportService.Models.Enums;
using ReportService.Service.BLL;

namespace ReportService.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class MarketingController : ControllerBase
{
    private readonly MarketingBLL _marketingBLL;
    public MarketingController(IServiceProvider provider)
    {
        _marketingBLL = provider.GetRequiredService<MarketingBLL>();
    }

    [HttpPost]
    public async Task<ApiResult<MarketingReportResponse>> DailyReportAsync(MarketingReportRequest request)
    {
        var data = await _marketingBLL.GetMarketingMarketingDailyReport(request);
        return new ApiResult<MarketingReportResponse>(ErrorCodeEnum.Success, data);
    }
}
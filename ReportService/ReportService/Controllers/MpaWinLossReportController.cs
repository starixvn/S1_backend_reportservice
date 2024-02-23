using Microsoft.AspNetCore.Mvc;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Models;
using ReportService.Models.Enums;
using ReportService.Service.BLL;

namespace ReportService.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class MpaWinLossReportController : ControllerBase
{
    private readonly MpaWinLossBLL _mpaWinLossBll;
    public MpaWinLossReportController(IServiceProvider provider)
    {
        _mpaWinLossBll = provider.GetRequiredService<MpaWinLossBLL>();
    }
    
    [HttpPost]
    public async Task<ApiResult<MpaWinLoseResponse>> GetAsync(MpaWinLossRequest request)
    {
        var data = await _mpaWinLossBll.GetMpaWinLossReportWithCache(request);

        return new ApiResult<MpaWinLoseResponse>(ErrorCodeEnum.Success, data);
    }
}
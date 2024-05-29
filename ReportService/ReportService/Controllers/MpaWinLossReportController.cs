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
    private readonly MpaWinLossImproveBLL _mpaWinLossImproveBll;
    public MpaWinLossReportController(IServiceProvider provider)
    {
        _mpaWinLossBll = provider.GetRequiredService<MpaWinLossBLL>();
        _mpaWinLossImproveBll = provider.GetRequiredService<MpaWinLossImproveBLL>();
    }

    [HttpPost]
    public async Task<ApiResult<MpaWinLoseResponse>> GetAsync(MpaWinLossRequest request)
    {
        if (request.Version is 0) //0=一般版本, 2=優化版本
        {
            //一般版本
            var data = await _mpaWinLossBll.GetMpaWinLossReportWithCache(request);

            return new ApiResult<MpaWinLoseResponse>(ErrorCodeEnum.Success, data);
        }
        else
        {
            //優化版本
            var data = await _mpaWinLossImproveBll.GetMpaWinLossReportWithCache(request);

            return new ApiResult<MpaWinLoseResponse>(ErrorCodeEnum.Success, data);
        }
    }
}
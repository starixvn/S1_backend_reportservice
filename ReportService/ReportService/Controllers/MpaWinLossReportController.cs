using Microsoft.AspNetCore.Mvc;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.ExceptionHandling;
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
    public ApiResult<object> Get(MpaWinLossRequest request)
    {
        var result = new ApiResult<object>(ErrorCodeEnum.Success, default);
        
        switch (request.GroupByType)
        {
            case MpaWinLossGroupByTypeEnum.Date:
            case MpaWinLossGroupByTypeEnum.Month:
                result.Data = _mpaWinLossBll.GetMpaWinLossGroupByDateReport(request);
                break;
            case MpaWinLossGroupByTypeEnum.Player:
                result.Data = _mpaWinLossBll.GetMpaWinLossGroupByPlayerReport(request);
                break;
            case MpaWinLossGroupByTypeEnum.Affiliate:
                result.Data = _mpaWinLossBll.GetMpaWinLossGroupByDateReport(request);
                break;
            case MpaWinLossGroupByTypeEnum.GameType:
                result.Data = _mpaWinLossBll.GetMpaWinLossGroupByGameTypeReport(request);
                break;
            case MpaWinLossGroupByTypeEnum.Currency:
                result.Data = _mpaWinLossBll.GetMpaWinLossGroupByTpCategoryReport(request);
                break;
            case MpaWinLossGroupByTypeEnum.TpCategory:
                result.Data = _mpaWinLossBll.GetMpaWinLossGroupByTpCategoryReport(request);
                break;
            case MpaWinLossGroupByTypeEnum.Brand:
            default:
                throw new ReportException("Not Supported GroupByType=" + request.GroupByType);
        }
   
        return result;
    }
}
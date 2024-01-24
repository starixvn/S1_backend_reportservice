using Microsoft.AspNetCore.Mvc;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Models;
using ReportService.Service.BLL;

namespace ReportService.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class MpaWinLossReportController : ControllerBase
{
    private readonly MpaWinLossBLL _mpaWinLossBll;
    private readonly Dictionary<MpaWinLossGroupByTypeEnum, Func<>> _mapper;
    public MpaWinLossReportController(IServiceProvider provider)
    {
        _mpaWinLossBll = provider.GetRequiredService<MpaWinLossBLL>();
    }
    
    [HttpPost]
    public ApiResult<MpaWinLoseResponse<>> Get(MpaWinLossRequest request)
    {
   
        return new ;
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportService.Core.Extensions;
using ReportService.Core.Library;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Service.DLL;

namespace ReportService.Core.Service.BLL;

public class MpaWinLossBLL
{
    private readonly MpaWinLossService _mpaWinLossService;
    private readonly ICacheHelper _redisHelper;
    private readonly ILogger<MpaWinLossBLL> _logger;
    
    public MpaWinLossBLL(IServiceProvider provider)
    {
        _mpaWinLossService = provider.GetRequiredService<MpaWinLossService>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<MpaWinLossBLL>>();
    }
    
    private async Task<IEnumerable<MpaTpBetRecordDto>> GetTpBetRecord(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        // 注單過濾
        var format = MapMpaWinLossGroupByTypeEnumFormat(groupByType);
        var data = await GetMapTpBetRecordByDate(request, groupByType);
        var tpBetRecordRelayResult = data.Filter(request)
            .SetCommissionByNineW(request, format, _mpaWinLossService.Get9WCommission)
            .SetBAdata()
            .ToList();

        // TODO: 在轉換時區前須先完成匯率轉換? => 先試試後轉會差到多少
        BetRecordSetDateToOffSet(request.StarTime.Offset, tpBetRecordRelayResult, groupByType);
        
        return tpBetRecordRelayResult;
    }
    
    private async Task<IEnumerable<MpaTpBetRecordDto>> GetMapTpBetRecordByDate(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(groupByType);
        var startDate = request.StarTime;
        var endDate = request.EndTime;
        var interval = TimeSpan.FromDays(16);
        var segmentCount = (int)Math.Ceiling((endDate - startDate).TotalDays / interval.TotalDays);
        var data = new List<MpaTpBetRecordDto>();
        var tasks = new List<Task<IEnumerable<MpaTpBetRecordDto>>>();
        for (var i = 0; i < segmentCount; i++)
        {
            var segmentStart = startDate.AddDays(i * interval.TotalDays);
            var segmentEnd = segmentStart.AddDays(interval.TotalDays);
            if (segmentEnd > endDate)
                segmentEnd = endDate;
            tasks.Add(Task.Run(() => _mpaWinLossService
                .GetTpBetRecord<MpaTpBetRecordDto>(request.SiteIds, startDate, endDate, format, request.MemberTypes, request.UserNames, request.DateType)));
        }

        await Task.WhenAll(tasks);
        foreach (var task in tasks)
        {
            data.AddRange(task.Result);
        }

        //為什麼會有這段
        //data = data.Where(x => !string.IsNullOrEmpty(x.UserName)).ToList();

        return data;
    }
    
    private static void BetRecordSetDateToOffSet(TimeSpan offset,
        List<MpaTpBetRecordDto> betRecordResult,
        MpaWinLossGroupByTypeEnum groupByType)
    {
        if (groupByType is not (MpaWinLossGroupByTypeEnum.Date or MpaWinLossGroupByTypeEnum.Month))
            return;
        
        betRecordResult.ForEach(item
            => { item.Date = item.Date.ToOffSetReturnDate(offset); });
    }
    
    private static string MapMpaWinLossGroupByTypeEnumFormat(MpaWinLossGroupByTypeEnum groupByType)
    {
        switch (groupByType)
        {
            case MpaWinLossGroupByTypeEnum.Date:
            case MpaWinLossGroupByTypeEnum.Month:
            case MpaWinLossGroupByTypeEnum.Brand:
                return "%m/%d/%Y %H:%i";
            
            case MpaWinLossGroupByTypeEnum.GameType:
            case MpaWinLossGroupByTypeEnum.TpCategory:
            case MpaWinLossGroupByTypeEnum.Player:
            case MpaWinLossGroupByTypeEnum.Currency:
            case MpaWinLossGroupByTypeEnum.Affiliate:
                return "%Y-%m-%d";
            default:
                return string.Empty;
        };
    }
}
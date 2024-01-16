using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportService.Core.Extensions;
using ReportService.Core.Library;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;
using ReportService.Core.Service.DLL;

namespace ReportService.Core.Service.BLL;

public class MpaWinLossBLL
{
    private readonly MpaWinLossService _mpaWinLossService;
    private readonly MpaCommonBLL _mpaCommonBll;
    private readonly ICacheHelper _redisHelper;
    private readonly ILogger<MpaWinLossBLL> _logger;
   
    public MpaWinLossBLL(IServiceProvider provider)
    {
        _mpaWinLossService = provider.GetRequiredService<MpaWinLossService>();
        _mpaCommonBll = provider.GetRequiredService<MpaCommonBLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<MpaWinLossBLL>>();
    
    }
    
    private async Task<TpBetRecordByDateWrapper> GetTpBetRecordGroupByDateResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var betRecordResult = await GetTpBetRecord(request, groupByType);
        //因GGR計算需用gameType分組，計算完再處理回來
        var groupResultByCurrency = GetGroupResultByCurrency(GetGroupResult(betRecordResult));
        
        IEnumerable<TpBetRecordGroupByDate> GetGroupResult(IEnumerable<MpaTpBetRecordDto> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.Date,
                    x.CurrencyId,
                    x.GameType,
                    x.CustId,
                    x.SiteId
                })
                .Select(r =>
                {
                    var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                    // 建立新的MpaWinLossByDateDto物件並返回
                    return new TpBetRecordGroupByDate(groupBySummary)
                    {
                        SiteId = r.Key.SiteId,
                        CustId = r.Key.CustId,
                        Date = r.Key.Date
                    };
                });
        }

        IEnumerable<TpBetRecordGroupByDate> GetGroupResultByCurrency(IEnumerable<TpBetRecordGroupByDate> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.Date,
                    x.CurrencyId,
                    x.CustId,
                    x.SiteId
                })
                .Select(r =>
                {
                    var groupBySummary = GetGroupSummary(r, r.Key.CurrencyId);
                    
                    return new TpBetRecordGroupByDate(groupBySummary)
                    {
                        SiteId = r.Key.SiteId,
                        CustId = r.Key.CustId,
                        Date = r.Key.Date
                    };
                });
        }

        var wrapperResult = new TpBetRecordByDateWrapper
        {
            TpBetRecordResult = groupResultByCurrency,
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };

        return wrapperResult;
    }
    
    private async Task<TpBetRecordByTpGameTypeWrapper> GetTpBetRecordGroupByTpGameTypeResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var tpGameTypeMap = await _mpaCommonBll.GetTpGameTypeMapper();
        var betRecordResult = await GetTpBetRecord(request, groupByType);
        var result = GetGroupResult(betRecordResult);

        IEnumerable<TpBetRecordGroupByTpGameType> GetGroupResult(IEnumerable<MpaTpBetRecordDto> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.CurrencyId,
                    x.GameType,
                }).Select(r =>
                {
                    var gameType = r.Key.GameType;
                    var groupBySummary = GetSummary(r, gameType, r.Key.CurrencyId);

                    return new TpBetRecordGroupByTpGameType(groupBySummary, tpGameTypeMap[r.Key.GameType]);
                });
        }
        
        var wrapperResult = new TpBetRecordByTpGameTypeWrapper()
        {
            TpBetRecordResult = result,
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };

        return wrapperResult;
    }
    
    private async Task<TpBetRecordByTpCategoryWrapper> GetTpBetRecordGroupByTpCategoryResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
        {
            var tpCategoryMap = await _mpaCommonBll.GetTpCategoryMapper();
            var betRecordResult = await GetTpBetRecord(request, groupByType);
            var tpBetRecordResult = GetGroupResultByCurrency(GetGroupResult(betRecordResult));

            IEnumerable<TpBetRecordGroupByTpCategory> GetGroupResult(IEnumerable<MpaTpBetRecordDto> source)
            {
                return source
                    .GroupBy(x => new
                    {
                        x.CategoryId,
                        x.CurrencyId,
                        x.GameType,
                    }).Select(r =>
                    {
                        var gameType = r.Key.GameType;
                        var groupBySummary = GetSummary(r, gameType, r.Key.CurrencyId);

                        return new TpBetRecordGroupByTpCategory(groupBySummary, tpCategoryMap[r.Key.CategoryId]);
                    });
            }
            
            IEnumerable<TpBetRecordGroupByTpCategory> GetGroupResultByCurrency(IEnumerable<TpBetRecordGroupByTpCategory> source)
            {
                return source
                    .GroupBy(x => new
                    {
                        x.TpCategoryName,
                        x.CurrencyId,
                    })
                    .Select(r =>
                    {
                        var groupBySummary = GetGroupSummary(r, r.Key.CurrencyId);
                        
                        return new TpBetRecordGroupByTpCategory(groupBySummary, r.Key.TpCategoryName);
                    });
            }
            
            var wrapperResult = new TpBetRecordByTpCategoryWrapper()
            {
                TpBetRecordResult = tpBetRecordResult,
                UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
            };

            return wrapperResult;
        }
    
    private async Task<List<MpaTpBetRecordDto>> GetTpBetRecord(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        // 注單過濾
        var format = MapMpaWinLossGroupByTypeEnumFormat(groupByType);
        var data = await GetMapTpBetRecordByDate(request, groupByType);
        var tpBetRecordRelayResult = data.Filter(request)
            .SetCommissionByNineW(request, format, _mpaWinLossService.Get9WCommission)
            .SetBAdata()
            .ToList();

        // TODO: 在轉換時區前須先完成匯率轉換? => 先試試後轉會差到多少 => 好像還是這邊比較好搞嗯嗯嗯?
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
                .GetTpBetRecord<MpaTpBetRecordDto>(new GetTpBetRecordParam(request, format, segmentStart, segmentEnd))));
        }

        await Task.WhenAll(tasks);
        foreach (var task in tasks)
        {
            data.AddRange(task.Result);
        }

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
        }
    }
    
    private static decimal CalculateGgr(int gameType, decimal winLossSummary, decimal progressiveWin, decimal progressiveBet, IEnumerable<MpaTpBetRecordDto> records)
    {
        switch (gameType)
        {
            case (int)MpaGgrCustomizeGameTypeEnum.NineW:
                //GGR for 9W = -PlayerWinLoss + Commission
                var nineWCommission = records.Sum(x => x.RevenueSummary);
                return GetGgr(winLossSummary, progressiveWin, progressiveBet) + nineWCommission;
            
            case (int)MpaGgrCustomizeGameTypeEnum.OneGamingPoker:
            case (int)MpaGgrCustomizeGameTypeEnum.V8Poker:
                //若為 Poker，commission: GGR = sum(revenue)
                winLossSummary = records.Sum(x => x.RevenueSummary) * -1; // 因為GetGgr內計算會 * -1所以乘回來
                return GetGgr(winLossSummary, progressiveWin, progressiveBet);
            
            default:
                return GetGgr(winLossSummary, progressiveWin, progressiveBet);
        }
    }
    
    private static decimal CalculateCommission(MpaTpBetRecordDto x)
    {
        //目前除9W外，其他commission皆為0
        if (x.TpId == (int)MpaCommissionCustomizeTpIdEnum.NineW)
        {
            return x.RevenueSummary * -1;
        }
        
        return 0;
    }

    private static decimal GetGgr(decimal winLossSummary, decimal progressiveWinSummary, decimal progressiveBetSummary)
    {
        return winLossSummary * -1 + (progressiveWinSummary - progressiveBetSummary);
    }

    private static ITpBetRecordGroupBy GetSummary(IEnumerable<MpaTpBetRecordDto> records, int gameType, CurrencyEnum currencyId)
    {
        var r = records.ToList();
        var totalStakeSummary = r.Sum(x => x.StakeSummary);
        var totalValidBetSummary = r.Sum(x => x.ValidBetSummary);
        var totalWinLossSummary = r.Sum(x => x.WinLossSummary);
        var totalProgressiveWinSummary = r.Sum(x => x.ProgressiveWinSummary);
        var totalProgressiveBetSummary = r.Sum(x => x.ProgressiveBetSummary);
        var totalRowCount = r.Sum(x => x.RowCount);
        var totalBaCommission = r.Sum(x => x.BaValidBetSummary) * 0.0025m; // BACommission = ValidTurnOver * 0.25%
        var totalBaWinLossSummary = r.Sum(x => x.BaWinLossSummary);
        var totalWinLossSummaryWithoutBa = totalWinLossSummary - totalBaWinLossSummary;
        var ggr = CalculateGgr(gameType, totalWinLossSummaryWithoutBa, totalProgressiveWinSummary,
            totalProgressiveBetSummary, r) + totalBaCommission;
        
        return new TpBetRecordGroupBy
        {
            CurrencyId = currencyId,
            TurnOver = totalStakeSummary,
            ValidTurnOver = totalValidBetSummary,
            Ggr = ggr,
            PlayerWinLoss = totalWinLossSummaryWithoutBa,
            BaPlayerWinLoss = totalBaWinLossSummary,
            ProgressiveWin = totalProgressiveWinSummary,
            ProgressiveBet = totalProgressiveBetSummary,
            Commission = r.Sum(CalculateCommission),
            BaCommission = totalBaCommission,
            BetCount = totalRowCount,
            AvgBetAmount = totalRowCount != 0 ? totalStakeSummary / totalRowCount : 0.00m,
            Margin = totalValidBetSummary != 0 ? ggr / totalValidBetSummary : 0.00m,
            UniquePlayers = r.DistinctBy(x => x.CustId).Count(),
        };
    }
    
    private static ITpBetRecordGroupBy GetGroupSummary(IEnumerable<ITpBetRecordGroupBy> groupBySummary, CurrencyEnum currencyId)
    {
        var r = groupBySummary.ToList();
        
        return new TpBetRecordGroupByTpCategory
        {
            CurrencyId = currencyId,
            TurnOver = r.Sum(x => x.TurnOver),
            ValidTurnOver = r.Sum(x => x.ValidTurnOver),
            Ggr = r.Sum(x => x.Ggr),
            PlayerWinLoss = r.Sum(x => x.PlayerWinLoss),
            BaPlayerWinLoss = r.Sum(x =>x.BaPlayerWinLoss),
            ProgressiveWin = r.Sum(x => x.ProgressiveWin),
            ProgressiveBet = r.Sum(x => x.ProgressiveBet),
            Commission = r.Sum(x => x.Commission),
            BaCommission = r.Sum(x => x.BaCommission),
            BetCount = r.Sum(x => x.BetCount),
            AvgBetAmount = r.Sum(x => x.BetCount) != 0 ? r.Sum(x => x.TurnOver) / r.Sum(x => x.BetCount) : 0.00m,
            Margin = r.Sum(x => x.ValidTurnOver) != 0
                ? r.Sum(x => x.Ggr) / r.Sum(x => x.ValidTurnOver)
                : 0.00m,
            UniquePlayers = r.Sum(x => x.UniquePlayers),
        };
    }
}
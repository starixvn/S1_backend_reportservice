using Newtonsoft.Json;
using ReportService.Core.Extensions;
using ReportService.Core.Library;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Service.DAL;

namespace ReportService.Service.BLL;

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

    #region GroupByDate & Month

    private static string GetDateString(DateTime date, MpaWinLossGroupByTypeEnum groupByType)
        => groupByType switch
        {
            MpaWinLossGroupByTypeEnum.Month => date.ToString("yyyy-MM"),
            _ => date.ToString("yyyy-MM-dd")
        };
    
    public async Task<MpaWinLoseResponse<MpaWinLossGroupByResponse>> GetMpaWinLossGroupByDateReport(
        MpaWinLossRequest request)
    {
        var result = await GetMpaWinLossGroupByDateResult(request);
        var total = GetMpaGroupSummary(result.ExchangedResult, request.BaseCurrencyId, request.GroupByType);
        
        return new MpaWinLoseResponse<MpaWinLossGroupByResponse>
        {
            Result = result.Result.Select(x => new MpaWinLossGroupByResponse(x, GetDateString(x.Date, request.GroupByType))),
            ExchangedResult = result.Result.Select(x => new MpaWinLossGroupByResponse(x, GetDateString(x.Date, request.GroupByType))),
            Total = new MpaWinLossGroupByResponse(total, "Total")
        };
    }

    private async Task<MpaWinLossGroupByWrapper<MpaWinLossGroupByDate>> GetMpaWinLossGroupByDateResult(MpaWinLossRequest request)
    {
        var betRecords = (await PreProcessMpaWinLossGroupByDateResult(request)).ToList();
        var transactionResult = await GetTransactionGroupByDateResult(request);
        var claimBonusResult = await GetClaimBonusGroupByDateResult(request);

        var combineData = betRecords
            .Concat(transactionResult)
            .Concat(claimBonusResult)
            .ToList();
        
        // 處理時區
        SetDateToOffSet(request.StartTime.Offset, combineData);
        
        var combineResult = combineData
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
            })
            .Select(r =>
            {
                var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, request.GroupByType);

                return new MpaWinLossGroupByDate(groupSummary)
                {
                    Date = r.Key.Date,
                    UniquePlayers = r.Where(x => x.BetCount != 0)
                        .DistinctBy(x => x.CustId).Count(),
                };
            })
            .ToList();
        
        // 匯率轉換
        var mapRate = await _mpaCommonBll.GetMapRate(request.StartTime, request.EndTime, request.BaseCurrencyId);
        var exchangedResult = combineResult
            .Select(r =>
            {
                var rate = mapRate[$"{r.Date.DateMapperToRateDate()}{(int)r.CurrencyId}{(int)request.BaseCurrencyId}"];
                var exchangedResult = r.ExchangeMpaWinLossGroupBySummary(rate);
              
                return new MpaWinLossGroupByDate(exchangedResult)
                {
                    Date = r.Date
                };
            });

        #region GroupByMonth
        
        if (request.GroupByType == MpaWinLossGroupByTypeEnum.Month)
        {
            combineResult = GetGroupByMonthResult(combineResult).ToList();
            exchangedResult = GetGroupByMonthResult(exchangedResult);
        }

        IEnumerable<MpaWinLossGroupByDate> GetGroupByMonthResult(IEnumerable<MpaWinLossGroupByDate> source)
        {
            return source
                .GroupBy(x => new
                {
                    Date = new DateTime(x.Date.Year, x.Date.Month, 1),
                    x.CurrencyId,
                })
                .Select(r =>
                {
                    var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, request.GroupByType);

                    return new MpaWinLossGroupByDate(groupSummary)
                    {
                        Date = r.Key.Date,
                        UniquePlayers = betRecords
                            .Where(x => x.Date.Year == r.Key.Date.Year && x.Date.Month == r.Key.Date.Month)
                            .DistinctBy(x => x.CustId)
                            .Count(),
                    };
                });
        }

        #endregion

        var wrapperResult = new MpaWinLossGroupByWrapper<MpaWinLossGroupByDate>
        {
            Result = combineResult,
            ExchangedResult = exchangedResult,
            UniquePlayers = betRecords.DistinctBy(x => x.CustId).Count()
        };

        return wrapperResult;
    }
    
    private async Task<IEnumerable<MpaWinLossGroupByDate>> GetClaimBonusGroupByDateResult(MpaWinLossRequest request)
    {
        var bonusRecords = await GetClaimBonusRecords(request);
        var result = GetGroupResult(bonusRecords);

        IEnumerable<MpaWinLossGroupByDate> GetGroupResult(IEnumerable<MpaClaimBonusDto> source)
        {
            return source.GroupBy(x => new
            {
                x.Date,
                x.CurrencyId
            }).Select(r => new MpaWinLossGroupByDate
            {
                Date = r.Key.Date,
                CurrencyId = r.Key.CurrencyId,
                TotalBonus = r.Sum(x => x.BonusAmountClaim)
            }).ToList();
        }

        return result;
    }

    private async Task<IEnumerable<MpaWinLossGroupByDate>> GetTransactionGroupByDateResult(MpaWinLossRequest request)
    {
        var transactionRecords = await GetTransactionRecords(request);
        var result = GetGroupResult(transactionRecords);
        
        IEnumerable<MpaWinLossGroupByDate> GetGroupResult(IEnumerable<MpaTransactionRelayDto> source)
        {
            return source.GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.SummaryType
            })
            .Select(r =>
            {
                var data = new MpaWinLossGroupByDate
                {
                    Date = r.Key.Date,
                    CurrencyId = r.Key.CurrencyId
                };

                switch (r.Key.SummaryType)
                {
                    case TransactionRelaySummaryTypeEnum.Deposit:
                        data.DepositCount = r.Sum(x => x.RowCount);
                        data.TotalDepositAmount = r.Sum(x => x.Amount);
                        break;
                    
                    case TransactionRelaySummaryTypeEnum.Withdrawal:
                        data.WithdrawalCount = r.Sum(x => x.RowCount);
                        data.TotalWithdrawalAmount = r.Sum(x => x.Amount);
                        break;
                }
                
                return data;
            });
        }

        return result;
    }
    
    private async Task<IEnumerable<MpaWinLossGroupByDate>> PreProcessMpaWinLossGroupByDateResult(MpaWinLossRequest request)
    {
        var betRecordResult = await GetTpBetRecords(request);
        var result = betRecordResult
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.GameType, // GGR計算需用gameType分組
                x.CustId
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByDate(groupBySummary)
                {
                    CustId = r.Key.CustId,
                    Date = r.Key.Date
                };
            });
        
        return result;
    }

    #endregion

    #region GroupByTpGameType
    
    public async Task<MpaWinLoseResponse<MpaWinLossSimpleGroupByResponse>> GetMpaWinLossGroupByGameTypeReport(
        MpaWinLossRequest request)
    {
        var result = await GetMpaWinLossGroupByGameTypeResult(request);
        var total = GetMpaGroupSummary(result.ExchangedResult, request.BaseCurrencyId, request.GroupByType);
        
        return new MpaWinLoseResponse<MpaWinLossSimpleGroupByResponse>
        {
            Result = result.Result.Select(x => new MpaWinLossSimpleGroupByResponse(x, x.TpGameTypeName)),
            ExchangedResult = result.Result.Select(x => new MpaWinLossSimpleGroupByResponse(x, x.TpGameTypeName)),
            Total = new MpaWinLossSimpleGroupByResponse(total, "Total")
        };
    }

    private async Task<MpaWinLossGroupByWrapper<MpaWinLossGroupByGameType>> GetMpaWinLossGroupByGameTypeResult(MpaWinLossRequest request)
    {
        var tpGameTypeMap = await _mpaCommonBll.GetTpGameTypeMapper();
        var betRecordResult = await GetTpBetRecords(request);
        
        var preProcessResult = betRecordResult
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.CustId,
                x.Date
            }).Select(r =>
            {
                var gameType = r.Key.GameType;
                var groupBySummary = GetSummary(r, gameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByGameType(groupBySummary)
                {
                    Date = r.Key.Date,
                    CustId = r.Key.CustId,
                    TpGameTypeName = tpGameTypeMap[r.Key.GameType]
                };
            })
            .ToList();

        var mapRate = await _mpaCommonBll.GetMapRate(request.StartTime, request.EndTime, request.BaseCurrencyId);
        var exchangedResult = preProcessResult
            .Select(r =>
            {
                var rate = mapRate[$"{r.Date.DateMapperToRateDate()}{(int)r.CurrencyId}{(int)request.BaseCurrencyId}"];
                var exchangedResult = r.ExchangeMpaWinLossGroupBySummary(rate);
                
                return new MpaWinLossGroupByGameType(exchangedResult)
                {
                    TpGameTypeName = r.TpGameTypeName,
                    CustId = r.CustId
                };
            });

        IEnumerable<MpaWinLossGroupByGameType> GroupByResult(IEnumerable<MpaWinLossGroupByGameType> source)
            => source
                .GroupBy(x => new
                {
                    x.CurrencyId,
                    x.TpGameTypeName
                })
                .Select(r =>
                {
                    var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, request.GroupByType);

                    return new MpaWinLossGroupByGameType(groupSummary)
                    {
                        TpGameTypeName = r.Key.TpGameTypeName,
                        UniquePlayers = r.Where(x => x.BetCount != 0)
                            .DistinctBy(x => x.CustId).Count(),
                    };
                });
        
        return new MpaWinLossGroupByWrapper<MpaWinLossGroupByGameType>
        {
            Result = GroupByResult(preProcessResult),
            ExchangedResult = GroupByResult(exchangedResult),
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };
    }

    #endregion

    #region GroupByTpCategory

    public async Task<MpaWinLoseResponse<MpaWinLossSimpleGroupByResponse>> GetMpaWinLossGroupByTpCategoryReport(
        MpaWinLossRequest request)
    {
        var result = await PreProcessMpaWinLossGroupByTpCategoryResult(request);
        var total = GetMpaGroupSummary(result.ExchangedResult, request.BaseCurrencyId, request.GroupByType);
        
        return new MpaWinLoseResponse<MpaWinLossSimpleGroupByResponse>
        {
            Result = result.Result.Select(x => new MpaWinLossSimpleGroupByResponse(x, x.TpCategoryName)),
            ExchangedResult = result.Result.Select(x => new MpaWinLossSimpleGroupByResponse(x, x.TpCategoryName)),
            Total = new MpaWinLossSimpleGroupByResponse(total, "Total")
        };
    }
    
    private async Task<MpaWinLossGroupByWrapper<MpaWinLossGroupByTpCategory>> PreProcessMpaWinLossGroupByTpCategoryResult(MpaWinLossRequest request)
    {
        var tpCategoryMap = await _mpaCommonBll.GetTpCategoryMapper();
        var betRecordResult = await GetTpBetRecords(request);
        var preProcessResult = betRecordResult
            .GroupBy(x => new
            {
                x.CategoryId,
                x.CurrencyId,
                x.GameType,
                x.Date,
                x.CustId
            }).Select(r =>
            {
                var gameType = r.Key.GameType;
                var groupBySummary = GetSummary(r, gameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByTpCategory(groupBySummary)
                {
                    TpCategoryName = tpCategoryMap[r.Key.CategoryId],
                    CustId = r.Key.CustId,
                    Date = r.Key.Date
                };
            })
            .ToList();
        
        var mapRate = await _mpaCommonBll.GetMapRate(request.StartTime, request.EndTime, request.BaseCurrencyId);
        var exchangedResult = preProcessResult
            .Select(r =>
            {
                var rate = mapRate[$"{r.Date.DateMapperToRateDate()}{(int)r.CurrencyId}{(int)request.BaseCurrencyId}"];
                var exchangedResult = r.ExchangeMpaWinLossGroupBySummary(rate);
                
                return new MpaWinLossGroupByTpCategory(exchangedResult)
                {
                    TpCategoryName = r.TpCategoryName,
                    CustId = r.CustId
                };
            });

        IEnumerable<MpaWinLossGroupByTpCategory> GroupByResult(IEnumerable<MpaWinLossGroupByTpCategory> source)
            => source
                .GroupBy(x => new
                {
                    x.CurrencyId,
                    x.TpCategoryName
                })
                .Select(r =>
                {
                    var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, request.GroupByType);

                    return new MpaWinLossGroupByTpCategory(groupSummary)
                    {
                        TpCategoryName = r.Key.TpCategoryName,
                        UniquePlayers = r.Where(x => x.BetCount != 0)
                            .DistinctBy(x => x.CustId).Count(),
                    };
                });
        
        return new MpaWinLossGroupByWrapper<MpaWinLossGroupByTpCategory>
        {
            Result = GroupByResult(preProcessResult),
            ExchangedResult = GroupByResult(exchangedResult),
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };
    }

    #endregion

    #region GroupByPlayer

    public async Task<MpaWinLoseResponse<MpaWinLossGroupByResponse>> GetMpaWinLossGroupByPlayerReport(
        MpaWinLossRequest request)
    {
        var result = await GetMpaWinLossGroupByDateResult(request);
        var total = GetMpaGroupSummary(result.ExchangedResult, request.BaseCurrencyId, request.GroupByType);
        
        return new MpaWinLoseResponse<MpaWinLossGroupByResponse>
        {
            Result = result.Result.Select(x => new MpaWinLossGroupByResponse(x, GetDateString(x.Date, request.GroupByType))),
            ExchangedResult = result.Result.Select(x => new MpaWinLossGroupByResponse(x, GetDateString(x.Date, request.GroupByType))),
            Total = new MpaWinLossGroupByResponse(total, "Total")
        };
    }
    
    private async Task<MpaWinLossGroupByWrapper<MpaWinLossGroupByPlayer>> GetMpaWinLossGroupByPlayerResult(MpaWinLossRequest request)
    {
        // todo: 原本的UserName當key如果不同site最後會被groupby在一起，暫時先用TpUserName
        var betRecordResult = await GetTpBetRecords(request);
        var preProcessResult = betRecordResult
            .GroupBy(x => new
            {
                x.UserName,
                x.TpUserName,
                x.CurrencyId,
                x.SiteId,
                x.GameType,
                x.Date
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByPlayer(groupBySummary)
                {
                    SiteId = r.Key.SiteId,
                    UserName = r.Key.UserName,
                    TpUserName = r.Key.TpUserName,
                    Date = r.Key.Date
                };
            });

        var mapRate = await _mpaCommonBll.GetMapRate(request.StartTime, request.EndTime, request.BaseCurrencyId);
        var exchangedResult = preProcessResult
            .Select(r =>
            {
                var rate = mapRate[$"{r.Date.DateMapperToRateDate()}{(int)r.CurrencyId}{(int)request.BaseCurrencyId}"];
                var exchangedResult = r.ExchangeMpaWinLossGroupBySummary(rate);
                
                return new MpaWinLossGroupByPlayer(exchangedResult)
                {
                    TpUserName = r.TpUserName
                };
            });

        IEnumerable<MpaWinLossGroupByPlayer> GroupByResult(IEnumerable<MpaWinLossGroupByPlayer> source)
            => source
                .GroupBy(x => new
                {
                    x.CurrencyId,
                    x.TpUserName
                })
                .Select(r =>
                {
                    var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, request.GroupByType);

                    return new MpaWinLossGroupByPlayer(groupSummary)
                    {
                        TpUserName = r.Key.TpUserName,
                        UniquePlayers = r.Where(x => x.BetCount != 0)
                            .DistinctBy(x => x.TpUserName).Count(),
                    };
                });
        
        return new MpaWinLossGroupByWrapper<MpaWinLossGroupByPlayer>
        {
            Result = GroupByResult(preProcessResult),
            ExchangedResult = GroupByResult(exchangedResult),
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };
    }

    #endregion

    #region GroupByAffiliate

    private async Task<IEnumerable<MpaWinLossGroupByAffiliate>> PreProcessMpaWinLossGroupByAffiliateResult(MpaWinLossRequest request)
    {
        var betRecordResult = await GetTpBetRecords(request);
        var result = betRecordResult
            .GroupBy(x => new
            {
                x.UserName,
                x.TpUserName,
                x.AffiliateName,
                x.CurrencyId,
                x.GameType,
                x.Date
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByAffiliate(groupBySummary)
                {
                    Date = r.Key.Date,
                    UserName = r.Key.UserName,
                    TpUserName = r.Key.TpUserName,
                    AffiliateAccount = r.Key.AffiliateName
                };
            });

        return result;
    }
    
    #endregion

    #region GroupByCurrency

    private async Task<IEnumerable<MpaWinLossGroupByCurrency>> PreProcessMpaWinLossGroupByCurrencyResult(MpaWinLossRequest request)
    {
        var betRecordResult = await GetTpBetRecords(request);
        var result = betRecordResult
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.CustId,
                x.Date
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByCurrency(groupBySummary)
                {
                    Date = r.Key.Date,
                    CustId = r.Key.CustId
                };
            });
        
        return result;
    }
    
    #endregion
    
    private async Task<List<MpaTpBetRecordDto>> GetTpBetRecords(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var data = await GetMapTpBetRecordByDate(request);

        var tpBetRecordRelayResult = data
            .Filter(request)
            .SetCommissionByNineW(request, format, _mpaWinLossService.Get9WCommission)
            .SetBAdata()
            .ToList();
        
        return tpBetRecordRelayResult;
    }
    
    private async Task<List<MpaTransactionRelayDto>> GetTransactionRecords(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var data = 
            await _mpaWinLossService.GetTransaction<MpaTransactionRelayDto>(new GetTransactionParam(request, format));
        
        var transactionResult = data
            .FilterTestUser(request.IncludeTest)
            .ToList();
        
        return transactionResult;
    }
    
    private async Task<List<MpaClaimBonusDto>> GetClaimBonusRecords(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var data = 
            await _mpaWinLossService.GetClaimBonus<MpaClaimBonusDto>(new GetTransactionParam(request, format));
        
        var claimBonusResult = data
            .FilterTestUser(request.IncludeTest)
            .ToList();

        return claimBonusResult;
    }
    
    private async Task<IEnumerable<MpaTpBetRecordDto>> GetMapTpBetRecordByDate(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var startDate = request.StartTime;
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
    
    private static void SetDateToOffSet<T>(TimeSpan offset, List<T> betRecordResult) where T : IFixDateTimeOffset
    {
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

    private static IMpaWinLossGroupBySummary GetSummary(IEnumerable<MpaTpBetRecordDto> records, int gameType, CurrencyEnum currencyId)
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
        
        return new MpaWinLossGroupBySummary
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
            BetCount = totalRowCount
        };
    }
    
    private readonly IEnumerable<MpaWinLossGroupByTypeEnum> _summaryTransaction = new[]
    {
        MpaWinLossGroupByTypeEnum.Date,
        MpaWinLossGroupByTypeEnum.Month,
        MpaWinLossGroupByTypeEnum.Player,
        MpaWinLossGroupByTypeEnum.Affiliate
    };

    private IMpaWinLossGroupBySummary GetMpaGroupSummary(IEnumerable<IMpaWinLossGroupBySummary> groupBySummary,
        CurrencyEnum currencyId,
        MpaWinLossGroupByTypeEnum groupByType)
    {
        
        var r = groupBySummary.ToList();
        var ggrSum = r.Sum(x => x.Ggr);
        var turnOverSum = r.Sum(x => x.TurnOver);
        var betCountSum = r.Sum(x => x.BetCount);
        var validTurnOverSum = r.Sum(x => x.ValidTurnOver);

        var result = new MpaWinLossGroupBySummary
        {
            CurrencyId = currencyId,
            TurnOver = turnOverSum,
            ValidTurnOver = validTurnOverSum,
            Ggr = ggrSum,
            PlayerWinLoss = r.Sum(x => x.PlayerWinLoss),
            BaPlayerWinLoss = r.Sum(x => x.BaPlayerWinLoss),
            ProgressiveWin = r.Sum(x => x.ProgressiveWin),
            ProgressiveBet = r.Sum(x => x.ProgressiveBet),
            Commission = r.Sum(x => x.Commission),
            BaCommission = r.Sum(x => x.BaCommission),
            BetCount = betCountSum,
            AvgBetAmount = betCountSum != 0 ? turnOverSum / betCountSum : 0.00m,
            Margin = validTurnOverSum != 0 ? ggrSum / validTurnOverSum : 0.00m
        };

        if (_summaryTransaction.Contains(groupByType))
        {
            result.DepositCount = r.Sum(x => x.DepositCount);
            result.TotalDepositAmount = r.Sum(x => x.TotalDepositAmount);
            result.WithdrawalCount = r.Sum(x => x.WithdrawalCount);
            result.TotalWithdrawalAmount = r.Sum(x => x.TotalWithdrawalAmount);
            result.TotalBonus = r.Sum(x => x.TotalBonus);
        }
        
        return result;
    }
    
}
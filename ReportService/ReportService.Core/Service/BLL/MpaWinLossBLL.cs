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

    #region GroupByDate & Month

    private async Task<MpaWinLossGroupByWrapper<MpaWinLossGroupByDate>> GetMpaGroupByDateResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var betRecords = (await PreProcessMpaWinLossGroupBySummaryDateResult(request, groupByType)).ToList();
        var transactionResult = await GetTransactionGroupByDateResult(request, groupByType);
        var claimBonusResult = await GetClaimBonusGroupByDateResult(request, groupByType);

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
                var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId);

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
                var rate = mapRate[$"{r.Date.DateMapperToRateDate()}{(int)r.CurrencyId}{request.BaseCurrencyId}"];
            
                return new MpaWinLossGroupByDate(r, rate)
                {
                    Date = r.Date
                };
            });

        #region GroupByMonth
        
        if (groupByType == MpaWinLossGroupByTypeEnum.Month)
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
                    var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId);

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
    
    private async Task<IEnumerable<MpaWinLossGroupByDate>> GetClaimBonusGroupByDateResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var bonusRecords = await GetClaimBonusRecords(request, groupByType);
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

    private async Task<IEnumerable<MpaWinLossGroupByDate>> GetTransactionGroupByDateResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var transactionRecords = await GetTransactionRecords(request, groupByType);
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
    
    private async Task<IEnumerable<MpaWinLossGroupByDate>> PreProcessMpaWinLossGroupBySummaryDateResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var betRecordResult = await GetTpBetRecords(request, groupByType);
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
            
        /* GetGroupResult(betRecordResult);
        IEnumerable<MpaWinLossGroupByDate> GetGroupResult(IEnumerable<MpaTpBetRecordDto> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.Date,
                    x.CurrencyId,
                    x.GameType,
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
        }
        
        IEnumerable<MpaWinLossGroupByDate> GetGroupByDateResult(IEnumerable<MpaWinLossGroupByDate> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.Date,
                    x.CurrencyId,
                    x.CustId,
                    //x.SiteId
                })
                .Select(r =>
                {
                    var groupBySummary = GetGroupSummary(r, r.Key.CurrencyId);
                    
                    return new MpaWinLossGroupByDate(groupBySummary)
                    {
                        //SiteId = r.Key.SiteId,
                        CustId = r.Key.CustId,
                        Date = r.Key.Date
                    };
                });
        }*/

        return result;
    }

    #endregion

    #region GroupByTpGameType

    private async Task<TpBetRecordByTpGameTypeWrapper> GetMpaWinLossGroupBySummaryTpGameTypeResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var tpGameTypeMap = await _mpaCommonBll.GetTpGameTypeMapper();
        var betRecordResult = await GetTpBetRecords(request, groupByType);
        
        var result = betRecordResult
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
            }).Select(r =>
            {
                var gameType = r.Key.GameType;
                var groupBySummary = GetSummary(r, gameType, r.Key.CurrencyId);

                return new MpaWinLossGroupBySummaryTpGameType(groupBySummary, tpGameTypeMap[r.Key.GameType]);
            });

        var wrapperResult = new TpBetRecordByTpGameTypeWrapper()
        {
            TpBetRecordResult = result,
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };

        return wrapperResult;
    }

    #endregion

    #region GroupByTpCategory

    private async Task<TpBetRecordByTpCategoryWrapper> GetMpaWinLossGroupBySummaryTpCategoryResult(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var tpCategoryMap = await _mpaCommonBll.GetTpCategoryMapper();
        var betRecordResult = await GetTpBetRecords(request, groupByType);
        var tpBetRecordResult = GetGroupByTpCategoryResult(GetGroupResult(betRecordResult));

        IEnumerable<MpaWinLossGroupBySummaryTpCategory> GetGroupResult(IEnumerable<MpaTpBetRecordDto> source)
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

                    return new MpaWinLossGroupBySummaryTpCategory(groupBySummary, tpCategoryMap[r.Key.CategoryId]);
                });
        }
        
        IEnumerable<MpaWinLossGroupBySummaryTpCategory> GetGroupByTpCategoryResult(IEnumerable<MpaWinLossGroupBySummaryTpCategory> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.TpCategoryName,
                    x.CurrencyId,
                })
                .Select(r =>
                {
                    var groupBySummary = GetMpaGroupSummary(r, r.Key.CurrencyId);
                    
                    return new MpaWinLossGroupBySummaryTpCategory(groupBySummary, r.Key.TpCategoryName);
                });
        }
        
        var wrapperResult = new TpBetRecordByTpCategoryWrapper()
        {
            TpBetRecordResult = tpBetRecordResult,
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };

        return wrapperResult;
    }

    #endregion

    #region GroupByPlayer

    private async Task<TpBetRecordByPlayerWrapper> GetMpaWinLossGroupBySummaryPlayerResult(MpaWinLossRequest request,
        MpaWinLossGroupByTypeEnum groupByType)
    {
        var betRecordResult = await GetTpBetRecords(request, groupByType);
        var tpBetRecordResult = GetGroupResult(betRecordResult);

        IEnumerable<MpaWinLossGroupBySummaryPlayer> GetGroupResult(IEnumerable<MpaTpBetRecordDto> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.UserName,
                    x.TpUserName,
                    x.CurrencyId,
                    x.SiteId,
                    x.GameType,
                })
                .Select(r =>
                {
                    var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                    return new MpaWinLossGroupBySummaryPlayer(groupBySummary)
                    {
                        SiteId = r.Key.SiteId,
                        UserName = r.Key.UserName,
                        TpUserName = r.Key.TpUserName,
                        ClosingBalance = r.Sum(x => x.ClosingBalance)
                    };
                });
        }
        
        var wrapperResult = new TpBetRecordByPlayerWrapper()
        {
            TpBetRecordResult = tpBetRecordResult,
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };

        return wrapperResult;
    }

    #endregion

    #region GroupByAffiliate

    private async Task<TpBetRecordByAffiliateWrapper> GetMpaWinLossGroupBySummaryAffiliateResult(MpaWinLossRequest request,
        MpaWinLossGroupByTypeEnum groupByType)
    {
        var betRecordResult = await GetTpBetRecords(request, groupByType);
        var tpBetRecordResult = GetGroupResult(betRecordResult);

        IEnumerable<MpaWinLossGroupBySummaryAffiliate> GetGroupResult(IEnumerable<MpaTpBetRecordDto> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.UserName,
                    x.TpUserName,
                    x.AffiliateName,
                    x.CurrencyId,
                    x.SiteId,
                    x.GameType,
                })
                .Select(r =>
                {
                    var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                    return new MpaWinLossGroupBySummaryAffiliate(groupBySummary)
                    {
                        SiteId = r.Key.SiteId,
                        UserName = r.Key.UserName,
                        TpUserName = r.Key.TpUserName,
                        AffiliateAccount = r.Key.AffiliateName,
                        ClosingBalance = r.Sum(x => x.ClosingBalance)
                    };
                });
        }
        
        var wrapperResult = new TpBetRecordByAffiliateWrapper()
        {
            TpBetRecordResult = tpBetRecordResult,
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };

        return wrapperResult;
    }
    
    #endregion

    #region GroupByCurrency

    private async Task<TpBetRecordByCurrencyWrapper> GetMpaWinLossGroupBySummaryCurrencyResult(MpaWinLossRequest request,
        MpaWinLossGroupByTypeEnum groupByType)
    {
        var betRecordResult = await GetTpBetRecords(request, groupByType);
        var tpBetRecordResult = GetGroupResult(betRecordResult);

        IEnumerable<MpaWinLossGroupBySummaryCurrency> GetGroupResult(IEnumerable<MpaTpBetRecordDto> source)
        {
            return source
                .GroupBy(x => new
                {
                    x.CurrencyId,
                    x.GameType,
                    x.CustId
                })
                .Select(r =>
                {
                    var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                    return new MpaWinLossGroupBySummaryCurrency(groupBySummary)
                    {
                        CustId = r.Key.CustId,
                    };
                });
        }
        
        var wrapperResult = new TpBetRecordByCurrencyWrapper()
        {
            TpBetRecordResult = tpBetRecordResult,
            UniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count()
        };

        return wrapperResult;
    }
    
    #endregion
    
    private async Task<List<MpaTpBetRecordDto>> GetTpBetRecords(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(groupByType);
        var data = await GetMapTpBetRecordByDate(request, groupByType);
        var tpBetRecordRelayResult = data.Filter(request)
            .SetCommissionByNineW(request, format, _mpaWinLossService.Get9WCommission)
            .SetBAdata()
            .ToList();
        
        //var mapRate = await _mpaCommonBll.GetMapRate(request.StartTime, request.EndTime, request.BaseCurrencyId);

        
        //SetDateToOffSet(request.StartTime.Offset, tpBetRecordRelayResult, groupByType);
        
        return tpBetRecordRelayResult;
    }
    
    private async Task<List<MpaTransactionRelayDto>> GetTransactionRecords(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(groupByType);
        var data = 
            await _mpaWinLossService.GetTransaction<MpaTransactionRelayDto>(new GetTransactionParam(request, format));
        var tpBetRecordRelayResult = data
            .FilterTestUser(request.IncludeTest)
            .ToList();

        // TODO: 匯率
        // var mapRate = await _mpaCommonBll.GetMapRate(request.StartTime, request.EndTime, request.BaseCurrencyId);
        //
        //
        // SetDateToOffSet(request.StartTime.Offset, tpBetRecordRelayResult, groupByType);
        
        return tpBetRecordRelayResult;
    }
    
    private async Task<List<MpaClaimBonusDto>> GetClaimBonusRecords(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(groupByType);
        var data = 
            await _mpaWinLossService.GetClaimBonus<MpaClaimBonusDto>(new GetTransactionParam(request, format));
        var tpBetRecordRelayResult = data
            .FilterTestUser(request.IncludeTest)
            .ToList();

        // TODO: 匯率
        //var mapRate = await _mpaCommonBll.GetMapRate(request.StartTime, request.EndTime, request.BaseCurrencyId);

        
        //SetDateToOffSet(request.StartTime.Offset, tpBetRecordRelayResult, groupByType);
        
        return tpBetRecordRelayResult;
    }
    
    private async Task<IEnumerable<MpaTpBetRecordDto>> GetMapTpBetRecordByDate(MpaWinLossRequest request, MpaWinLossGroupByTypeEnum groupByType)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(groupByType);
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
        // if (groupByType is not (MpaWinLossGroupByTypeEnum.Date or MpaWinLossGroupByTypeEnum.Month))
        //     return;
        
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
            BetCount = totalRowCount,
            AvgBetAmount = totalRowCount != 0 ? totalStakeSummary / totalRowCount : 0.00m,
            Margin = totalValidBetSummary != 0 ? ggr / totalValidBetSummary : 0.00m,
            UniquePlayers = r.DistinctBy(x => x.CustId).Count(),
        };
    }
    
    private static IMpaWinLossGroupBySummary GetMpaGroupSummary(IEnumerable<IMpaWinLossGroupBySummary> groupBySummary, CurrencyEnum currencyId)
    {
        var r = groupBySummary.ToList();
        var ggrSum = r.Sum(x => x.Ggr);
        var turnOverSum = r.Sum(x => x.Ggr);
        var betCountSum = r.Sum(x => x.BetCount);
        var validTurnOverSum = r.Sum(x => x.ValidTurnOver);
        
        return new MpaWinLossGroupBySummary
        {
            CurrencyId = currencyId,
            TurnOver = turnOverSum,
            ValidTurnOver = validTurnOverSum,
            Ggr = ggrSum,
            PlayerWinLoss = r.Sum(x => x.PlayerWinLoss),
            BaPlayerWinLoss = r.Sum(x =>x.BaPlayerWinLoss),
            ProgressiveWin = r.Sum(x => x.ProgressiveWin),
            ProgressiveBet = r.Sum(x => x.ProgressiveBet),
            Commission = r.Sum(x => x.Commission),
            BaCommission = r.Sum(x => x.BaCommission),
            BetCount = betCountSum,
            AvgBetAmount = betCountSum != 0 ? turnOverSum / betCountSum : 0.00m,
            Margin = validTurnOverSum != 0 ? ggrSum / validTurnOverSum : 0.00m,
            DepositCount = r.Sum(x => x.DepositCount),
            TotalDepositAmount = r.Sum(x => x.TotalDepositAmount),
            WithdrawalCount = r.Sum(x => x.WithdrawalCount),
            TotalWithdrawalAmount = r.Sum(x => x.TotalWithdrawalAmount),
            TotalBonus = r.Sum(x => x.TotalBonus)
        };
    }
    
}
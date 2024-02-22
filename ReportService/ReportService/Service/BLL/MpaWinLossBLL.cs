using System.Diagnostics;
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
    private readonly Dictionary<MpaWinLossGroupByTypeEnum, Func<MpaWinLossRequest, Task<(List<MpaWinLossGroupByModel>, int)>>> _dicPreProcessMapper;
   
    public MpaWinLossBLL(IServiceProvider provider)
    {
        _mpaWinLossService = provider.GetRequiredService<MpaWinLossService>();
        _mpaCommonBll = provider.GetRequiredService<MpaCommonBLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<MpaWinLossBLL>>();
    
        _dicPreProcessMapper = new Dictionary<MpaWinLossGroupByTypeEnum, Func<MpaWinLossRequest, Task<(List<MpaWinLossGroupByModel>, int)>>>
        {
            {MpaWinLossGroupByTypeEnum.Brand, PreProcessMpaWinLossGroupByBrand},
            {MpaWinLossGroupByTypeEnum.Currency, PreProcessMpaWinLossGroupByCurrency},
            {MpaWinLossGroupByTypeEnum.TpCategory, PreProcessMpaWinLossGroupByTpCategory},
            {MpaWinLossGroupByTypeEnum.GameType, PreProcessMpaWinLossGroupByGameType},
            {MpaWinLossGroupByTypeEnum.Affiliate, PreProcessMpaWinLossGroupByAffiliate},
            {MpaWinLossGroupByTypeEnum.Player, PreProcessMpaWinLossGroupByPlayer},
            {MpaWinLossGroupByTypeEnum.Date, PreProcessMpaWinLossGroupByDateResult},
            {MpaWinLossGroupByTypeEnum.Month, PreProcessMpaWinLossGroupByDateResult}
        };
    
    }

    #region GroupByDate & Month

    private static string GetDateString(DateTime date, MpaWinLossGroupByTypeEnum groupByType)
        => groupByType switch
        {
            MpaWinLossGroupByTypeEnum.Month => date.ToString("yyyy-MM"),
            _ => date.ToString("yyyy-MM-dd")
        };
    
    public async Task<MpaWinLoseResponse> GetMpaWinLossReport(MpaWinLossRequest request)
    {
        var childResult = await GetMpaWinLossGroupByChildResult(request);
        var result = GetMpaWinLossGroupByResult(childResult.ExchangedResult, request.GroupByType, request.BaseCurrencyId);
        var total = GetMpaGroupSummary(childResult.ExchangedResult, request.BaseCurrencyId, request.GroupByType);
     
        return new MpaWinLoseResponse
        {
            Result = result,
            ChildResult = request.DisplayGroupBreakdownInOriginalCurrency
                ? childResult.Result
                : childResult.ExchangedResult,
            Total = new MpaWinLossGroupByModel(total)
            {
                Key = "Total",
                ChildKey = "All"
            }
        };
    }
    
    private async Task<(List<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByDateResult(MpaWinLossRequest request)
    {
        var betRecordTask = Task.Run(() => GetTpBetRecords(request));
        var transactionTask = Task.Run(() => GetTransactionGroupByDateResult(request));
        var claimBonusTask = Task.Run(() => GetClaimBonusGroupByDateResult(request));
        
        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);
        
        var betRecordResult = betRecordTask.Result 
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

                return new MpaWinLossGroupByModel(groupBySummary)
                {
                    CustId = r.Key.CustId,
                    Date = r.Key.Date
                };
            })
            .ToList();

        var combineData = betRecordResult
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result)
            .ToList();

        // 處理時區
        SetDateToOffSet(request.StartTime.Offset, combineData);
        
        var combineResult = combineData
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.CustId
            })
            .Select(r =>
            {
                var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, request.GroupByType);

                return new MpaWinLossGroupByModel(groupSummary)
                {
                    Date = r.Key.Date,
                    CustId = r.Key.CustId,
                    Key = GetDateString(r.Key.Date, request.GroupByType),
                    ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId)
                };
            })
            .ToList();

        var uniquePlayers = betRecordTask.Result.DistinctBy(x => x.CustId).Count();

        return (combineResult, uniquePlayers);
    }
    
    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetClaimBonusGroupByDateResult(MpaWinLossRequest request)
    {
        var bonusRecords = await GetClaimBonusRecords(request);
        var result = bonusRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId
            }).Select(r => new MpaWinLossGroupByModel
            {
                Date = r.Key.Date,
                CurrencyId = r.Key.CurrencyId,
                TotalBonus = r.Sum(x => x.BonusAmountClaim)
            });

        return result;
    }

    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetTransactionGroupByDateResult(MpaWinLossRequest request)
    {
        var transactionRecords = await GetTransactionRecords(request);
        var result = transactionRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.SummaryType
            })
            .Select(r =>
            {
                var data = new MpaWinLossGroupByModel
                {
                    Date = r.Key.Date,
                    CurrencyId = r.Key.CurrencyId
                };

                SummaryByTransactionSummaryType(data, r, r.Key.SummaryType);
                
                return data;
            });
        
        return result;
    }

    #endregion

    #region GroupByTpGameType
    
    private async Task<(List<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByGameType(MpaWinLossRequest request)
    {
        var tpGameTypeMap = await _mpaCommonBll.GetTpGameTypeMapper();
        var betRecordResult = await GetTpBetRecords(request);
        
        var preProcessResult = betRecordResult
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.Date
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByModel(groupBySummary)
                {
                    Date = r.Key.Date,
                    Key = tpGameTypeMap[r.Key.GameType],
                    ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId)
                };
            })
            .ToList();

        var uniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count();

        return (preProcessResult, uniquePlayers);
    }

    #endregion

    #region GroupByTpCategory

    private async Task<(List<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByTpCategory(MpaWinLossRequest request)
    {
        var tpCategoryMap = await _mpaCommonBll.GetTpCategoryMapper();
        var betRecordResult = await GetTpBetRecords(request);
        
        var preProcessResult = betRecordResult
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.Date,
                x.CategoryId
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByModel(groupBySummary)
                {
                    Date = r.Key.Date,
                    Key = tpCategoryMap[r.Key.CategoryId],
                    ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId)
                };
            })
            .ToList();

        var uniquePlayers = betRecordResult.DistinctBy(x => x.CustId).Count();

        return (preProcessResult, uniquePlayers);
    }

    #endregion

    #region GroupByPlayer

    private async Task<(List<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByPlayer(MpaWinLossRequest request)
    {
        var betRecordTask = Task.Run(() => GetTpBetRecords(request));
        var transactionTask = Task.Run(() => GetTransactionGroupByPlayerResult(request));
        var claimBonusTask = Task.Run(() => GetClaimBonusGroupByPlayerResult(request));
        
        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);
        
        var preProcessResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.CustId,
                x.Date,
                x.TpUserName,
                x.UserName
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByModel(groupBySummary)
                {
                    Date = r.Key.Date,
                    CustId = r.Key.CustId,
                    Key = r.Key.TpUserName,
                    UserName = r.Key.UserName,
                    TpUserName = r.Key.TpUserName,
                    ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId)
                };
            })
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result)
            .ToList();

        var uniquePlayers = betRecordTask.Result.DistinctBy(x => x.CustId).Count();

        return (preProcessResult, uniquePlayers);
    }
    
    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetClaimBonusGroupByPlayerResult(MpaWinLossRequest request)
    {
        var bonusRecords = await GetClaimBonusRecords(request);
        var result = bonusRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.TpUserName,
                x.UserName,
                x.CustId
            }).Select(r => new MpaWinLossGroupByModel
            {
                Date = r.Key.Date,
                CurrencyId = r.Key.CurrencyId,
                CustId = r.Key.CustId,
                UserName = r.Key.UserName,
                TpUserName = r.Key.TpUserName,
                Key = r.Key.TpUserName,
                ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId),
                TotalBonus = r.Sum(x => x.BonusAmountClaim)
            });

        return result;
    }

    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetTransactionGroupByPlayerResult(MpaWinLossRequest request)
    {
        var transactionRecords = await GetTransactionRecords(request);
        var result = transactionRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CustId,
                x.CurrencyId,
                x.SummaryType,
                x.TpUserName,
                x.UserName
            })
            .Select(r =>
            {
                var data = new MpaWinLossGroupByModel
                {
                    Date = r.Key.Date,
                    CustId = r.Key.CustId,
                    CurrencyId = r.Key.CurrencyId,
                    Key = r.Key.TpUserName,
                    UserName = r.Key.UserName,
                    TpUserName = r.Key.TpUserName,
                    ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId),
                };

                SummaryByTransactionSummaryType(data, r, r.Key.SummaryType);
                
                return data;
            });
        
        return result;
    }

    #endregion

    #region GroupByAffiliate
    
    private async Task<(List<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByAffiliate(MpaWinLossRequest request)
    {
        var betRecordTask = Task.Run(() => GetTpBetRecords(request));
        var transactionTask = Task.Run(() => GetTransactionGroupByAffiliateResult(request));
        var claimBonusTask = Task.Run(() => GetClaimBonusGroupByAffiliateResult(request));
        
        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);
        
        var preProcessResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.CustId,
                x.Date,
                x.UserName,
                x.TpUserName,
                x.AffiliateName
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByModel(groupBySummary)
                {
                    Date = r.Key.Date,
                    CustId = r.Key.CustId,
                    Key = r.Key.AffiliateName,
                    UserName = r.Key.UserName,
                    TpUserName = r.Key.TpUserName,
                    ChildKey = r.Key.UserName
                };
            })
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result)
            .ToList();

        var uniquePlayers = betRecordTask.Result.DistinctBy(x => x.CustId).Count();

        return (preProcessResult, uniquePlayers);
    }

    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetClaimBonusGroupByAffiliateResult(MpaWinLossRequest request)
    {
        var bonusRecords = await GetClaimBonusRecords(request);
        var result = bonusRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.CustId,
                x.TpUserName,
                x.UserName,
                x.AffiliateName
            }).Select(r => new MpaWinLossGroupByModel
            {
                Date = r.Key.Date,
                CustId = r.Key.CustId,
                CurrencyId = r.Key.CurrencyId,
                Key = r.Key.AffiliateName,
                UserName = r.Key.UserName,
                TpUserName = r.Key.TpUserName,
                ChildKey = r.Key.UserName,
                TotalBonus = r.Sum(x => x.BonusAmountClaim)
            });

        return result;
    }

    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetTransactionGroupByAffiliateResult(MpaWinLossRequest request)
    {
        var transactionRecords = await GetTransactionRecords(request);
        var result = transactionRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.SummaryType,
                x.CustId,
                x.TpUserName,
                x.UserName,
                x.AffiliateName
            })
            .Select(r =>
            {
                var data = new MpaWinLossGroupByModel
                {
                    Date = r.Key.Date,
                    CurrencyId = r.Key.CurrencyId,
                    CustId = r.Key.CustId,
                    UserName = r.Key.UserName,
                    TpUserName = r.Key.TpUserName,
                    Key = r.Key.AffiliateName,
                    ChildKey = r.Key.UserName
                };

                SummaryByTransactionSummaryType(data, r, r.Key.SummaryType);
                
                return data;
            });
        
        return result;
    }

    #endregion

    #region GroupByCurrency
    
    private async Task<(List<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByCurrency(MpaWinLossRequest request)
    {
        var betRecordTask = Task.Run(() => GetTpBetRecords(request));
        var transactionTask = Task.Run(() => GetTransactionGroupByCurrencyResult(request));
        var claimBonusTask = Task.Run(() => GetClaimBonusGroupByCurrencyResult(request));
        
        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);
        
        var preProcessResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.Date
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);
                var currency = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);

                return new MpaWinLossGroupByModel(groupBySummary)
                {
                    Date = r.Key.Date,
                    Key = currency,
                    ChildKey = currency
                };
            })
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result)
            .ToList();

        var uniquePlayers = betRecordTask.Result.DistinctBy(x => x.CustId).Count();

        return (preProcessResult, uniquePlayers);
    }
    
    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetClaimBonusGroupByCurrencyResult(MpaWinLossRequest request)
    {
        var bonusRecords = await GetClaimBonusRecords(request);
        var result = bonusRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId
            }).Select(r =>
            {
                var currency = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);
                return new MpaWinLossGroupByModel
                {
                    Date = r.Key.Date,
                    CurrencyId = r.Key.CurrencyId,
                    Key = currency,
                    ChildKey = currency,
                    TotalBonus = r.Sum(x => x.BonusAmountClaim)
                };
            });

        return result;
    }

    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetTransactionGroupByCurrencyResult(MpaWinLossRequest request)
    {
        var transactionRecords = await GetTransactionRecords(request);
        var result = transactionRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.SummaryType
            })
            .Select(r =>
            {
                var currency = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);
                var data = new MpaWinLossGroupByModel
                {
                    Date = r.Key.Date,
                    CurrencyId = r.Key.CurrencyId,
                    Key = currency,
                    ChildKey = currency
                };

                SummaryByTransactionSummaryType(data, r, r.Key.SummaryType);
                
                return data;
            });
        
        return result;
    }

    #endregion

    #region GroupByBrand

    private async Task<(List<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByBrand(MpaWinLossRequest request)
    {
        var betRecordTask = Task.Run(() => GetTpBetRecords(request));
        var transactionTask = Task.Run(() => GetTransactionGroupByBrandResult(request));
        var claimBonusTask = Task.Run(() => GetClaimBonusGroupByBrandResult(request));
        
        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);
        
        var preProcessResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.Date,
                x.SiteId
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.GameType, r.Key.CurrencyId);

                return new MpaWinLossGroupByModel(groupBySummary)
                {
                    Date = r.Key.Date,
                    Key = _mpaCommonBll.ToSiteName(r.Key.SiteId),
                    ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId)
                };
            })
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result)
            .ToList();

        var uniquePlayers = betRecordTask.Result.DistinctBy(x => x.CustId).Count();

        return (preProcessResult, uniquePlayers);
    }
    
    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetClaimBonusGroupByBrandResult(MpaWinLossRequest request)
    {
        var bonusRecords = await GetClaimBonusRecords(request);
        var result = bonusRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.SiteId
            }).Select(r =>
                new MpaWinLossGroupByModel
                {
                    Date = r.Key.Date,
                    CurrencyId = r.Key.CurrencyId,
                    Key = _mpaCommonBll.ToSiteName(r.Key.SiteId),
                    ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId),
                    TotalBonus = r.Sum(x => x.BonusAmountClaim)
                });

        return result;
    }

    private async Task<IEnumerable<MpaWinLossGroupByModel>> GetTransactionGroupByBrandResult(MpaWinLossRequest request)
    {
        var transactionRecords = await GetTransactionRecords(request);
        var result = transactionRecords
            .GroupBy(x => new
            {
                x.Date,
                x.CurrencyId,
                x.SummaryType,
                x.SiteId
            })
            .Select(r =>
            {
                var data = new MpaWinLossGroupByModel
                {
                    Date = r.Key.Date,
                    CurrencyId = r.Key.CurrencyId,
                    Key = _mpaCommonBll.ToSiteName(r.Key.SiteId),
                    ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId)
                };

                SummaryByTransactionSummaryType(data, r, r.Key.SummaryType);
                
                return data;
            });
        
        return result;
    }
    
    #endregion
    
    private async Task<MpaWinLossGroupByWrapper> GetMpaWinLossGroupByChildResult(MpaWinLossRequest request)
    {
        var (preProcessResult, uniquePlayers) =
            await _dicPreProcessMapper[request.GroupByType].Invoke(request);

        var exchangedResult = await ExchangeRate(preProcessResult, request);

        return new MpaWinLossGroupByWrapper
        {
            Result = GroupByResult(preProcessResult, request.GroupByType),
            ExchangedResult = GroupByResult(exchangedResult, request.GroupByType),
            UniquePlayers = uniquePlayers
        };
    }

    private IEnumerable<MpaWinLossGroupByModel> GetMpaWinLossGroupByResult(IEnumerable<MpaWinLossGroupByModel> source,
        MpaWinLossGroupByTypeEnum groupByType, int baseCurrencyId)
    {
        if (groupByType == MpaWinLossGroupByTypeEnum.Player)
            return source;
        
        return source.GroupBy(x => new
        {
            x.Key
        }).Select(r =>
        {
            var groupSummary = GetMpaGroupSummary(r, baseCurrencyId, groupByType);

            return new MpaWinLossGroupByModel(groupSummary)
            {
                Key = r.Key.Key,
                ChildKey = "All",
                UniquePlayers = r.Sum(x => x.UniquePlayers)
            };
        });
    }

    private async Task<IEnumerable<MpaWinLossGroupByModel>> ExchangeRate(IEnumerable<MpaWinLossGroupByModel> source,
        MpaWinLossRequest request)
    {
        var mapRate = await _mpaCommonBll.GetMapRate(request.StartTime, request.EndTime, request.BaseCurrencyId);
        var exchangedResult = source
            .Select(r =>
            {
                var rate = mapRate[$"{r.Date.DateMapperToRateDate()}{r.CurrencyId}{request.BaseCurrencyId}"];
                var exchangedResult = r.ExchangeMpaWinLossGroupBySummary(rate);
                
                return new MpaWinLossGroupByModel(exchangedResult)
                {
                    Key = r.Key,
                    ChildKey = r.ChildKey,
                    Date = r.Date,
                    CustId = r.CustId,
                    TpUserName = r.TpUserName,
                    UserName = r.UserName
                };
            });

        return exchangedResult;
    }

    private readonly IEnumerable<MpaWinLossGroupByTypeEnum> _hasPlayerInfo = new[]
    {
        MpaWinLossGroupByTypeEnum.Player,
        MpaWinLossGroupByTypeEnum.Affiliate
    };

    private IEnumerable<MpaWinLossGroupByModel> GroupByResult(IEnumerable<MpaWinLossGroupByModel> source,
        MpaWinLossGroupByTypeEnum groupByType)
    {
        var hasPlayerInfo = _hasPlayerInfo.Contains(groupByType);
        
        var g = source.GroupBy(x => new
            {
                x.CurrencyId,
                x.Key,
                x.ChildKey
            });

        if (!hasPlayerInfo)
        {
            return g.Select(r =>
            {
                var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, groupByType);

                return new MpaWinLossGroupByModel(groupSummary)
                {
                    Key = r.Key.Key,
                    ChildKey = r.Key.ChildKey,
                    UniquePlayers = r.Where(x => x.BetCount != 0)
                        .DistinctBy(x => x.CustId).Count(),
                };
            });
        }

        return g.Select(r =>
        {
            var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, groupByType);
            var f = r.First();

            return new MpaWinLossGroupByModel(groupSummary)
            {
                Key = r.Key.Key,
                ChildKey = r.Key.ChildKey,
                UserName = f.UserName,
                TpUserName = f.TpUserName,
                UniquePlayers = r.Where(x => x.BetCount != 0)
                    .DistinctBy(x => x.CustId).Count(),
            };
        });
    }
    
    private async Task<List<MpaTpBetRecordDto>> GetTpBetRecords(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var data = await GetTpBetRecordsWithCache(request);

        var tpBetRecordRelayResult = data
            .Filter(request)
            .SetCommissionByNineW(request, format, _mpaWinLossService.Get9WCommission)
            .SetBAdata()
            .ToList();
        
        return tpBetRecordRelayResult;
    }
    
    private async Task<IEnumerable<MpaTpBetRecordDto>> GetTpBetRecordsWithCache(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var p = new GetTpBetRecordParam(request, string.Empty, request.StartTime, request.EndTime);

        if (string.IsNullOrEmpty(p.UserNamesJson) && string.IsNullOrEmpty(p.AffiliateNamesJson))
        {
            var key = $"{p.StartTime:yyyyMMddHHmmss}{p.EndTime:yyyyMMddHHmmss}{format}" +
                      $"{request.DateType}{p.SiteIdsJson}{p.CurrencyIdsJson}{p.MemberTypesJson}" +
                      $"{p.CategoryIdsJson}{p.GameTypesJson}";

            return await _redisHelper.TryGetOrCreateAsync(key, TimeSpan.FromMinutes(10),
                () => GetMapTpBetRecordByDate(request));
        }
        
        return await GetMapTpBetRecordByDate(request);
    }
    
    private async Task<List<MpaTransactionRelayDto>> GetTransactionRecords(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var data = 
            await _mpaWinLossService.GetTransaction<MpaTransactionRelayDto>(new GetTransactionParam(request, format));
        
        var transactionResult = data
            .FilterTestUser(request.IncludeTest)
            .FilterAffiliate(request)
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
            .FilterAffiliate(request)
            .ToList();

        return claimBonusResult;
    }
    
    private async Task<IEnumerable<MpaTpBetRecordDto>> GetMapTpBetRecordByDate(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var startDate = request.StartTime;
        var endDate = request.EndTime;
        var interval = TimeSpan.FromDays(10);
        var segmentCount = (int)Math.Ceiling((endDate - startDate).TotalDays / interval.TotalDays);
        var data = new List<MpaTpBetRecordDto>();
        var tasks = new List<Task<IEnumerable<MpaTpBetRecordDto>>>();
        var sw = Stopwatch.StartNew();
        
        _logger.LogInformation($"GetMapTpBetRecordByDate segmentCount={segmentCount}");
        
        for (var i = 0; i < segmentCount; i++)
        {
            var segmentStart = startDate.AddDays(i * interval.TotalDays);
            var segmentEnd = segmentStart.AddDays(interval.TotalDays);
            if (segmentEnd > endDate)
                segmentEnd = endDate;
            
            _logger.LogInformation($"SegmentStart={segmentStart:yyyy-MM-dd HH:mm:ss}, SegmentEnd={segmentEnd:yyyy-MM-dd HH:mm:ss}");
            
            tasks.Add(Task.Run(() => _mpaWinLossService
                .GetTpBetRecord<MpaTpBetRecordDto>(new GetTpBetRecordParam(request, format, segmentStart, segmentEnd))));
        }

        await Task.WhenAll(tasks)
            .ContinueWith(f =>
            {
                _logger.LogInformation(
                    $"GetMapTpBetRecordByDate Start={request.StartTime:yyyy-MM-dd HH:mm:sszzz}, End={request.EndTime:yyyy-MM-dd HH:mm:sszzz} {sw.Elapsed:c}");
            });
        
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

    private static void SummaryByTransactionSummaryType(MpaWinLossGroupBySummary data,
        IEnumerable<MpaTransactionRelayDto> records, TransactionRelaySummaryTypeEnum summaryType)
    {
        var r = records.ToList();
        switch (summaryType)
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
    }

    private static MpaWinLossGroupBySummary GetSummary(IEnumerable<MpaTpBetRecordDto> records, int gameType, int currencyId)
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
        MpaWinLossGroupByTypeEnum.Affiliate,
        MpaWinLossGroupByTypeEnum.Brand,
        MpaWinLossGroupByTypeEnum.Currency
    };

    private MpaWinLossGroupBySummary GetMpaGroupSummary(IEnumerable<MpaWinLossGroupBySummary> groupBySummary,
        int currencyId,
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
using System;
using System.Diagnostics;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using ReportService.Core.Extensions;
using ReportService.Core.Library;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Extensions;
using ReportService.Service.DAL;

namespace ReportService.Service.BLL;

public class MpaWinLossImproveBLL
{
    private readonly MpaWinLossService _mpaWinLossService;
    private readonly MpaCommonBLL _mpaCommonBll;
    private readonly ICacheHelper _redisHelper;
    private readonly ILogger<MpaWinLossImproveBLL> _logger;
    private readonly Dictionary<MpaWinLossGroupByTypeEnum, Func<MpaWinLossRequest, Task<(IEnumerable<MpaWinLossGroupByModel>, int)>>> _dicPreProcessMapper;

    public MpaWinLossImproveBLL(IServiceProvider provider)
    {
        _mpaWinLossService = provider.GetRequiredService<MpaWinLossService>();
        _mpaCommonBll = provider.GetRequiredService<MpaCommonBLL>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<MpaWinLossImproveBLL>>();

        _dicPreProcessMapper = new Dictionary<MpaWinLossGroupByTypeEnum, Func<MpaWinLossRequest, Task<(IEnumerable<MpaWinLossGroupByModel>, int)>>>
        {
            {MpaWinLossGroupByTypeEnum.Brand, PreProcessMpaWinLossGroupByBrand},
            {MpaWinLossGroupByTypeEnum.Currency, PreProcessMpaWinLossGroupByCurrency},
            {MpaWinLossGroupByTypeEnum.TpCategory, PreProcessMpaWinLossGroupByTpCategory},
            {MpaWinLossGroupByTypeEnum.GameType, PreProcessMpaWinLossGroupByGameType},
            {MpaWinLossGroupByTypeEnum.Affiliate, PreProcessMpaWinLossGroupByAffiliate},
            {MpaWinLossGroupByTypeEnum.Player, PreProcessMpaWinLossGroupByPlayer},
            {MpaWinLossGroupByTypeEnum.Date, PreProcessMpaWinLossGroupByDateResult},
            {MpaWinLossGroupByTypeEnum.Month, PreProcessMpaWinLossGroupByDateResult},
            {MpaWinLossGroupByTypeEnum.DateDownload, PreProcessMpaWinLossGroupByDateDownloadResult},
            {MpaWinLossGroupByTypeEnum.MonthDownload, PreProcessMpaWinLossGroupByDateDownloadResult}
        };
    }

    public async Task<MpaWinLoseResponse> GetMpaWinLossReportWithCache(MpaWinLossRequest request)
    {
        var key = JsonConvert.SerializeObject(request);
        var min = MpaWinLossDefinition.UseLongerCache.Contains(request.GroupByType) ? 20 : 5; // Player類有分頁，分頁是在後台做所以給長點時間

        return await _redisHelper.TryGetOrCreateAsync(key, TimeSpan.FromMinutes(min),
                () => GetMpaWinLossReport(request));
    }

    private async Task<MpaWinLoseResponse> GetMpaWinLossReport(MpaWinLossRequest request)
    {
        var childResult = await GetMpaWinLossGroupByChildResult(request);
        var result = GetMpaWinLossGroupByResult(childResult.ExchangedResult, request.GroupByType,
            request.BaseCurrencyId);
        var total = GetMpaWinLossReportTotalResult(childResult.ExchangedResult, childResult.UniquePlayers, request.BaseCurrencyId, request.GroupByType);

        if (request.GroupByType is MpaWinLossGroupByTypeEnum.DateDownload
            || request.GroupByType is MpaWinLossGroupByTypeEnum.MonthDownload)
        {
            return new MpaWinLoseResponse
            {
                Result = request.DisplayGroupBreakdownInOriginalCurrency
                ? _mpaCommonBll.ConvertToOriginalCurrency(childResult.Result)
                : childResult.ExchangedResult,
                ChildResult = new List<MpaWinLossGroupByModel>(),
                Total = total
            };
        }

        return new MpaWinLoseResponse
        {
            Result = result,
            ChildResult = request.DisplayGroupBreakdownInOriginalCurrency
                ? _mpaCommonBll.ConvertToOriginalCurrency(childResult.Result)
                : childResult.ExchangedResult,
            Total = total
        };
    }

    private MpaWinLossGroupByModel GetMpaWinLossReportTotalResult(IEnumerable<MpaWinLossGroupByModel> exchangedResult, int uniquePlayers, int baseCurrencyId, MpaWinLossGroupByTypeEnum groupByType)
    {
        var total = GetMpaGroupSummary(exchangedResult, baseCurrencyId, groupByType);

        total.Key = "total";
        total.ChildKey = "All";
        total.UniquePlayers = uniquePlayers;

        return total;
    }

    #region GroupByDate & Month

    private static string GetDateString(DateTime date, MpaWinLossGroupByTypeEnum groupByType)
        => groupByType switch
        {
            MpaWinLossGroupByTypeEnum.Month => date.ToString("yyyy-MM"),
            MpaWinLossGroupByTypeEnum.MonthDownload => date.ToString("yyyy-MM"),
            _ => date.ToString("yyyy-MM-dd")
        };

    private static DateTime GetStringDate(string dateStr, DateTime date, MpaWinLossGroupByTypeEnum groupByType)
    {
        if (groupByType is MpaWinLossGroupByTypeEnum.MonthDownload)
        {
            DateTime dateTime = DateTime.ParseExact(dateStr, "yyyy-MM", null);
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }
        return date;
    }

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByDateResult(MpaWinLossRequest request)
    {
        var betRecordTask = GetTpBetRecords(request);
        var transactionTask = GetTransactionGroupByDateResult(request);
        var claimBonusTask = GetClaimBonusGroupByDateResult(request);

        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);

        var preProcessedGroups = betRecordTask.Result.GroupAndSummarize(
            x => new { x.CurrencyId, x.Date, x.CustId }
            );

        var betRecordResult = preProcessedGroups
            .Select(g =>
            {
                var summary = GetSummary(g.GroupKey.CurrencyId, g.Summaries);
                summary.Date = g.GroupKey.Date;
                summary.CustId = g.GroupKey.CustId;
                return summary;
            });

        var combineData = betRecordResult
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result)
            .AsParallel()
            .Select(item =>
            {
                // 處理時區
                item.Date = item.Date.ToOffSetReturnDate(request.StartTime.Offset, true);
                return item;
            }).ToList();

        var preProcessedMpaGroups = combineData.MpaGroupAndSummarize(
            x => new { x.CurrencyId, x.Date, x.CustId }
            );
        var isSummaryTransaction = IsSummaryTransaction(request.GroupByType);

        var combineResult = preProcessedMpaGroups
            .Select(g =>
            {
                var summary = GetMpaGroupSummary(g.GroupKey.CurrencyId, g.Summaries, isSummaryTransaction);
                summary.Date = g.GroupKey.Date;
                summary.CustId = g.GroupKey.CustId;
                summary.Key = GetDateString(g.GroupKey.Date, request.GroupByType);
                summary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(g.GroupKey.CurrencyId);
                return summary;
            });

        var uniquePlayersCount = new HashSet<int>(betRecordTask.Result.Select(x => x.CustId)).Count;

        return (combineResult, uniquePlayersCount);
    }

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByDateResultBackUp(MpaWinLossRequest request)
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
                x.CustId
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.CurrencyId);

                groupBySummary.CustId = r.Key.CustId;
                groupBySummary.Date = r.Key.Date;

                return groupBySummary;
            });

        var combineData = betRecordResult
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result)
            .ToList();

        // 處理時區
        foreach (var item in combineData)
        {
            item.Date = item.Date.ToOffSetReturnDate(request.StartTime.Offset, true);
        }

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

                   groupSummary.Date = r.Key.Date;
                   groupSummary.CustId = r.Key.CustId;
                   groupSummary.Key = GetDateString(r.Key.Date, request.GroupByType);
                   groupSummary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);

                   return groupSummary;
               });

        var uniquePlayersCount = new HashSet<int>(betRecordTask.Result.Select(x => x.CustId)).Count;
        return (combineResult, uniquePlayersCount);
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

    #region GroupByDate & Month Download

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByDateDownloadResult(MpaWinLossRequest request)
    {
        var tpGameTypeMap = await _mpaCommonBll.GetTpGameTypeMapper();
        var tpCategoryMap = await _mpaCommonBll.GetTpCategoryMapper();
        var betRecordResult = await GetTpBetRecords(request);

        var preProcessedGroups = betRecordResult.GroupAndSummarize(
            x => new
            {
                x.Date,
                x.SiteId,
                x.CurrencyId,
                x.CustId,
                x.GameType, // GGR計算需用gameType分組
                x.CategoryId,
                x.TpUserName,
                x.UserName
            }
        );

        var preProcessResult = preProcessedGroups
            .Select(g =>
            {
                var groupBySummary = GetSummary(g.GroupKey.CurrencyId, g.Summaries);
                groupBySummary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(g.GroupKey.CurrencyId);
                groupBySummary.Date = g.GroupKey.Date;
                groupBySummary.CustId = g.GroupKey.CustId;
                groupBySummary.Brand = _mpaCommonBll.ToSiteName(g.GroupKey.SiteId);
                groupBySummary.TpCategory = tpCategoryMap[g.GroupKey.CategoryId];
                groupBySummary.UserName = g.GroupKey.UserName;
                groupBySummary.TpUserName = g.GroupKey.TpUserName;
                groupBySummary.TpGameType = tpGameTypeMap[g.GroupKey.GameType];
                return groupBySummary;
            });

        var ProcessResult = preProcessResult
            .AsParallel()
            .Select(item =>
            {
                // 處理時區
                item.Date = item.Date.ToOffSetReturnDate(request.StartTime.Offset, true);
                return item;
            }).ToList();

        var uniquePlayersCount = new HashSet<int>(betRecordResult.Select(x => x.CustId)).Count;
        return (ProcessResult, uniquePlayersCount);
    }


    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByDateDownloadResultBackUp(MpaWinLossRequest request)
    {
        var betRecordTask = Task.Run(() => GetTpBetRecords(request));
        var tpCategoryMap = await _mpaCommonBll.GetTpCategoryMapper();
        var tpGameTypeMap = await _mpaCommonBll.GetTpGameTypeMapper();
        await Task.WhenAll(betRecordTask);

        var betRecordResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.Date,
                x.SiteId,
                x.CurrencyId,
                x.CustId,
                x.GameType, // GGR計算需用gameType分組
                x.CategoryId,
                x.TpUserName,
                x.UserName
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.CurrencyId);

                groupBySummary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);
                groupBySummary.Date = r.Key.Date;
                groupBySummary.CustId = r.Key.CustId;
                groupBySummary.Brand = _mpaCommonBll.ToSiteName(r.Key.SiteId);
                groupBySummary.TpCategory = tpCategoryMap[r.Key.CategoryId];
                groupBySummary.UserName = r.Key.UserName;
                groupBySummary.TpUserName = r.Key.TpUserName;
                groupBySummary.TpGameType = tpGameTypeMap[r.Key.GameType];

                return groupBySummary;
            });

        var combineData = betRecordResult.ToList();

        // 處理時區
        foreach (var item in combineData)
        {
            item.Date = item.Date.ToOffSetReturnDate(request.StartTime.Offset, true);
        }

        var combineResult = combineData
               .GroupBy(x => new
               {
                   Date = GetStringDate(GetDateString(x.Date, request.GroupByType), x.Date, request.GroupByType),
                   x.Brand,
                   x.CurrencyId,
                   x.CustId,
                   x.TpGameType,
                   x.TpCategory,
                   x.TpUserName,
                   x.UserName
               })
               .Select(r =>
               {
                   var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, request.GroupByType);

                   groupSummary.Key = GetDateString(r.Key.Date, request.GroupByType);
                   groupSummary.Date = r.Key.Date;
                   groupSummary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);
                   groupSummary.CustId = r.Key.CustId;
                   groupSummary.Brand = r.Key.Brand;
                   groupSummary.TpCategory = r.Key.TpCategory;
                   groupSummary.UserName = r.Key.UserName;
                   groupSummary.TpUserName = r.Key.TpUserName;
                   groupSummary.TpGameType = r.Key.TpGameType;
                   groupSummary.UniquePlayers = 1;
                   return groupSummary;
               });

        var uniquePlayersCount = new HashSet<int>(betRecordTask.Result.Select(x => x.CustId)).Count;
        return (combineResult, uniquePlayersCount);
    }

    #endregion

    #region GroupByTpGameType

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByGameType(MpaWinLossRequest request)
    {
        var tpGameTypeMap = await _mpaCommonBll.GetTpGameTypeMapper();
        var betRecordResult = await GetTpBetRecords(request);

        var preProcessedGroups = betRecordResult.GroupAndSummarize(
            x => new { x.CurrencyId, x.GameType, x.Date, x.CustId }
        );

        var preProcessResult = preProcessedGroups
            .Select(g =>
            {
                var summary = GetSummary(g.GroupKey.CurrencyId, g.Summaries);
                summary.Date = g.GroupKey.Date;
                summary.Key = tpGameTypeMap[g.GroupKey.GameType];
                summary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(g.GroupKey.CurrencyId);
                summary.CustId = g.GroupKey.CustId;
                return summary;
            });

        var uniquePlayersCount = new HashSet<int>(betRecordResult.Select(x => x.CustId)).Count;
        return (preProcessResult, uniquePlayersCount);
    }

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByGameTypeBackUp(MpaWinLossRequest request)
    {
        var tpGameTypeMap = await _mpaCommonBll.GetTpGameTypeMapper();
        var betRecordResult = await GetTpBetRecords(request);

        var preProcessResult = betRecordResult
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.Date,
                x.CustId
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.CurrencyId);

                groupBySummary.Date = r.Key.Date;
                groupBySummary.Key = tpGameTypeMap[r.Key.GameType];
                groupBySummary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);
                groupBySummary.CustId = r.Key.CustId;

                return groupBySummary;
            });

        var uniquePlayersCount = new HashSet<int>(betRecordResult.Select(x => x.CustId)).Count;
        return (preProcessResult, uniquePlayersCount);
    }

    #endregion

    #region GroupByTpCategory

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByTpCategory(MpaWinLossRequest request)
    {
        var tpCategoryMap = await _mpaCommonBll.GetTpCategoryMapper();
        var betRecordResult = await GetTpBetRecords(request);

        var preProcessResult = betRecordResult
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.Date,
                x.CategoryId,
                x.CustId
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.CurrencyId);

                groupBySummary.Date = r.Key.Date;
                groupBySummary.Key = tpCategoryMap[r.Key.CategoryId];
                groupBySummary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);
                groupBySummary.CustId = r.Key.CustId;

                return groupBySummary;
            });

        var uniquePlayersCount = new HashSet<int>(betRecordResult.Select(x => x.CustId)).Count;
        return (preProcessResult, uniquePlayersCount);
    }

    #endregion

    #region GroupByPlayer

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByPlayer(MpaWinLossRequest request)
    {
        var betRecordTask = GetTpBetRecords(request);
        var transactionTask = GetTransactionGroupByPlayerResult(request);
        var claimBonusTask = GetClaimBonusGroupByPlayerResult(request);

        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);

        var preProcessResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.CustId,
                x.Date,
                x.TpUserName,
                x.UserName
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.CurrencyId);

                groupBySummary.Date = r.Key.Date;
                groupBySummary.CustId = r.Key.CustId;
                groupBySummary.Key = r.Key.TpUserName;
                groupBySummary.UserName = r.Key.UserName;
                groupBySummary.TpUserName = r.Key.TpUserName;
                groupBySummary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);

                return groupBySummary;
            })
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result);

        var uniquePlayersCount = new HashSet<int>(betRecordTask.Result.Select(x => x.CustId)).Count;
        return (preProcessResult, uniquePlayersCount);
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
            })
            .Select(r => new MpaWinLossGroupByModel
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

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByAffiliate(MpaWinLossRequest request)
    {
        var betRecordTask = GetTpBetRecords(request);
        var transactionTask = GetTransactionGroupByAffiliateResult(request);
        var claimBonusTask = GetClaimBonusGroupByAffiliateResult(request);

        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);

        var preProcessResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.CustId,
                x.Date,
                x.UserName,
                x.TpUserName,
                x.AffiliateName
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.CurrencyId);

                groupBySummary.Date = r.Key.Date;
                groupBySummary.CustId = r.Key.CustId;
                groupBySummary.Key = r.Key.AffiliateName;
                groupBySummary.UserName = r.Key.UserName;
                groupBySummary.TpUserName = r.Key.TpUserName;
                groupBySummary.ChildKey = r.Key.UserName;

                return groupBySummary;
            })
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result);

        var uniquePlayersCount = new HashSet<int>(betRecordTask.Result.Select(x => x.CustId)).Count;
        return (preProcessResult, uniquePlayersCount);
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

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByCurrency(MpaWinLossRequest request)
    {
        var betRecordTask = GetTpBetRecords(request);
        var transactionTask = GetTransactionGroupByCurrencyResult(request);
        var claimBonusTask = GetClaimBonusGroupByCurrencyResult(request);

        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);

        var preProcessResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.GameType,
                x.Date,
                x.CustId
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.CurrencyId);
                var currency = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);

                groupBySummary.Date = r.Key.Date;
                groupBySummary.Key = currency;
                groupBySummary.ChildKey = currency;
                groupBySummary.CustId = r.Key.CustId;

                return groupBySummary;
            })
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result);

        var uniquePlayersCount = new HashSet<int>(betRecordTask.Result.Select(x => x.CustId)).Count;
        return (preProcessResult, uniquePlayersCount);
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

    private async Task<(IEnumerable<MpaWinLossGroupByModel>, int)> PreProcessMpaWinLossGroupByBrand(MpaWinLossRequest request)
    {
        var betRecordTask = GetTpBetRecords(request);
        var transactionTask = GetTransactionGroupByBrandResult(request);
        var claimBonusTask = GetClaimBonusGroupByBrandResult(request);

        await Task.WhenAll(betRecordTask, transactionTask, claimBonusTask);

        var preProcessResult = betRecordTask.Result
            .GroupBy(x => new
            {
                x.CurrencyId,
                x.Date,
                x.SiteId,
                x.CustId
            })
            .Select(r =>
            {
                var groupBySummary = GetSummary(r, r.Key.CurrencyId);

                groupBySummary.Date = r.Key.Date;
                groupBySummary.Key = _mpaCommonBll.ToSiteName(r.Key.SiteId);
                groupBySummary.ChildKey = _mpaCommonBll.ToCurrencyIsoCode(r.Key.CurrencyId);
                groupBySummary.CustId = r.Key.CustId;

                return groupBySummary;
            })
            .Concat(transactionTask.Result)
            .Concat(claimBonusTask.Result);

        var uniquePlayersCount = new HashSet<int>(betRecordTask.Result.Select(x => x.CustId)).Count;
        return (preProcessResult, uniquePlayersCount);
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
        if (groupByType == MpaWinLossGroupByTypeEnum.Player
            || groupByType == MpaWinLossGroupByTypeEnum.DateDownload
            || groupByType == MpaWinLossGroupByTypeEnum.MonthDownload)
            return source;

        var preProcessedMpaGroups = source.MpaGroupAndSummarize(
            x => new { x.Key }
            );
        var isSummaryTransaction = IsSummaryTransaction(groupByType);
        return preProcessedMpaGroups
            .Select(g =>
            {
                var groupSummary = GetMpaGroupSummary(baseCurrencyId, g.Summaries, isSummaryTransaction);
                groupSummary.Key = g.GroupKey.Key;
                groupSummary.ChildKey = "All";
                return groupSummary;
            });
    }

    private IEnumerable<MpaWinLossGroupByModel> GetMpaWinLossGroupByResultBackUp(IEnumerable<MpaWinLossGroupByModel> source,
    MpaWinLossGroupByTypeEnum groupByType, int baseCurrencyId)
    {
        if (groupByType == MpaWinLossGroupByTypeEnum.Player
            || groupByType == MpaWinLossGroupByTypeEnum.DateDownload
            || groupByType == MpaWinLossGroupByTypeEnum.MonthDownload)
            return source;

        return source.GroupBy(x => new
        {
            x.Key
        }).Select(r =>
        {
            var groupSummary = GetMpaGroupSummary(r, baseCurrencyId, groupByType);

            groupSummary.Key = r.Key.Key;
            groupSummary.ChildKey = "All";

            return groupSummary;
        });
    }

    private async Task<IEnumerable<MpaWinLossGroupByModel>> ExchangeRate(IEnumerable<MpaWinLossGroupByModel> source,
        MpaWinLossRequest request)
    {
        var adjustHour = 0;
        var hasDateTimeConvert = MpaWinLossDefinition.HasDateTimeConvert.Contains(request.GroupByType);
        if (hasDateTimeConvert)
        {
            adjustHour = (int)request.StartTime.Offset.TotalHours * -1;
        }
        var mapRate = await _mpaCommonBll.GetMapMonthRate(request.StartTime, request.EndTime, request.BaseCurrencyId);
        var exchangedResult = source
            .Select(r =>
            {
                //OANDA exchange rate is based on GMT -4
                var rate = mapRate[$"{r.Date.AddHours(adjustHour).AddHours(-4).DateMapperToRateMonth()}{r.CurrencyId}{request.BaseCurrencyId}"];
                r.ExchangeMpaWinLossGroupBySummary(rate);

                return r;
            });
        return exchangedResult;
    }

    private IEnumerable<MpaWinLossGroupByModel> GroupByResult(IEnumerable<MpaWinLossGroupByModel> source,
        MpaWinLossGroupByTypeEnum groupByType)
    {
        var hasPlayerInfo = MpaWinLossDefinition.HasPlayerInfo.Contains(groupByType);

        var preProcessedMpaGroups = source.MpaGroupAndSummarize(
              x => new { x.CurrencyId, x.Key, x.ChildKey }
              );

        if (groupByType == MpaWinLossGroupByTypeEnum.DateDownload || groupByType == MpaWinLossGroupByTypeEnum.MonthDownload)
            return source;

        var isSummaryTransaction = IsSummaryTransaction(groupByType);

        if (!hasPlayerInfo)
        {
            return preProcessedMpaGroups
                .Select(g =>
                {
                    var groupSummary = GetMpaGroupSummary(g.GroupKey.CurrencyId, g.Summaries, isSummaryTransaction);
                    groupSummary.Key = g.GroupKey.Key;
                    groupSummary.ChildKey = g.GroupKey.ChildKey;
                    groupSummary.UniquePlayers = g.Records.Where(x => x.BetCount != 0)
                        .DistinctBy(x => x.CustId).Count();
                    return groupSummary;
                });
        }

        var result = preProcessedMpaGroups
                .Select(g =>
                {
                    var groupSummary = GetMpaGroupSummary(g.GroupKey.CurrencyId, g.Summaries, isSummaryTransaction);
                    var f = g.Records.First();
                    groupSummary.Key = g.GroupKey.Key;
                    groupSummary.ChildKey = g.GroupKey.ChildKey;
                    groupSummary.CustId = f.CustId;
                    groupSummary.UserName = f.UserName;
                    groupSummary.TpUserName = f.TpUserName;
                    groupSummary.TpGameType = f.TpGameType;
                    groupSummary.TpCategory = f.TpCategory;
                    groupSummary.Brand = f.Brand;
                    groupSummary.UniquePlayers = 1;
                    return groupSummary;
                });
        return result;
    }

    private IEnumerable<MpaWinLossGroupByModel> GroupByResultBackUp(IEnumerable<MpaWinLossGroupByModel> source,
    MpaWinLossGroupByTypeEnum groupByType)
    {

        var hasPlayerInfo = MpaWinLossDefinition.HasPlayerInfo.Contains(groupByType);

        var g = source.GroupBy(x => new
        {
            x.CurrencyId,
            x.Key,
            x.ChildKey
        });


        if (groupByType == MpaWinLossGroupByTypeEnum.DateDownload || groupByType == MpaWinLossGroupByTypeEnum.MonthDownload)
            return source;

        if (!hasPlayerInfo)
        {
            return g.Select(r =>
            {
                var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, groupByType);

                groupSummary.Key = r.Key.Key;
                groupSummary.ChildKey = r.Key.ChildKey;
                groupSummary.UniquePlayers = r.Where(x => x.BetCount != 0)
                    .DistinctBy(x => x.CustId).Count();

                return groupSummary;
            });
        }

        var result = g.Select(r =>
        {
            var groupSummary = GetMpaGroupSummary(r, r.Key.CurrencyId, groupByType);
            var f = r.First();

            groupSummary.Key = r.Key.Key;
            groupSummary.ChildKey = r.Key.ChildKey;
            groupSummary.CustId = f.CustId;
            groupSummary.UserName = f.UserName;
            groupSummary.TpUserName = f.TpUserName;
            groupSummary.TpGameType = f.TpGameType;
            groupSummary.TpCategory = f.TpCategory;
            groupSummary.Brand = f.Brand;
            groupSummary.UniquePlayers = 1;

            return groupSummary;
        });

        return result;
    }

    #region Source

    private async Task<IEnumerable<MpaWinlossRecordDto>> GetTpBetRecords(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var data = await GetTpBetRecordsWithCache(request, format);

        return data;
    }

    private async Task<IEnumerable<MpaWinlossRecordDto>> GetTpBetRecordsWithCache(MpaWinLossRequest request, string format)
    {
        var p = new GetTpBetRecordParam(request, string.Empty, request.StartTime, request.EndTime);

        if (string.IsNullOrEmpty(p.UserNamesJson) && string.IsNullOrEmpty(p.AffiliateNamesJson))
        {
            var key = $"WinLossSummaryImprove{p.StartTime:yyyyMMddHHmmss}{p.EndTime:yyyyMMddHHmmss}{format}" +
                      $"{request.DateType}{p.SiteIdsJson}{p.CurrencyIdsJson}{p.MemberTypesJson}" +
                      $"{p.CategoryIdsJson}{p.GameTypesJson}";

            return await _redisHelper.TryGetOrCreateAsync(key, TimeSpan.FromMinutes(10),
                () => GetMapTpBetRecordByDate(request, format));
        }

        return await GetMapTpBetRecordByDate(request, format);
    }

    /// <summary>
    /// 全撈版本
    /// </summary>
    /// <param name="request"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    private async Task<IEnumerable<MpaWinlossRecordDto>> GetMapTpBetRecordByDate(MpaWinLossRequest request, string format)
    {
        var data = await _mpaWinLossService.GetWinLossSummary<MpaWinlossRecordDto>(new GetTpBetRecordParam(request, format));

        var tpBetRecordResult = data
            .FilterAffiliate(request)
            .SetCommissionAndGGRByNineWImprove(request);

        return tpBetRecordResult;
    }

    /// <summary>
    /// 天數分段取資料版本
    /// </summary>
    /// <param name="request"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    private async Task<IEnumerable<MpaWinlossRecordDto>> GetMapTpBetRecordByDateSeperateByDate(MpaWinLossRequest request, string format)
    {
        var startDate = request.StartTime;
        var endDate = request.EndTime;
        var interval = TimeSpan.FromDays(11);
        var segmentCount = (int)Math.Ceiling((endDate - startDate).TotalDays / interval.TotalDays);
        var data = new List<MpaWinlossRecordDto>();
        var tasks = new List<Task<IEnumerable<MpaWinlossRecordDto>>>();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation($"GetMapTpBetRecordByDate SegmentCount={segmentCount}");

        for (var i = 0; i < segmentCount; i++)
        {
            var segmentStart = startDate.AddDays(i * interval.TotalDays);
            var segmentEnd = segmentStart.AddDays(interval.TotalDays);
            if (segmentEnd > endDate)
                segmentEnd = endDate;

            _logger.LogInformation($"SegmentStart={segmentStart:yyyy-MM-dd HH:mm:ss}, SegmentEnd={segmentEnd:yyyy-MM-dd HH:mm:ss}");

            tasks.Add(Task.Run(() => _mpaWinLossService
                .GetWinLossSummary<MpaWinlossRecordDto>(new GetTpBetRecordParam(request, format, segmentStart, segmentEnd))));
        }

        await Task.WhenAll(tasks)
            .ContinueWith(_ =>
            {
                _logger.LogInformation(
                    $"GetMapTpBetRecordByDate Start={request.StartTime:yyyy-MM-ddTHH:mm:sszzz}, End={request.EndTime:yyyy-MM-ddTHH:mm:sszzz}, Elapsed={sw.Elapsed:c}");
            });

        foreach (var task in tasks)
        {
            data.AddRange(task.Result);
        }

        return data;
    }

    private async Task<IEnumerable<MpaTransactionRelayDto>> GetTransactionRecords(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var data =
            await _mpaWinLossService.GetTransaction<MpaTransactionRelayDto>(new GetTransactionParam(request, format));

        var transactionResult = data
            .FilterTestUser(request.IncludeTest)
            .FilterAffiliate(request);

        return transactionResult;
    }

    private async Task<IEnumerable<MpaClaimBonusDto>> GetClaimBonusRecords(MpaWinLossRequest request)
    {
        var format = MapMpaWinLossGroupByTypeEnumFormat(request.GroupByType);
        var data =
            await _mpaWinLossService.GetClaimBonus<MpaClaimBonusDto>(new GetTransactionParam(request, format));

        var claimBonusResult = data
            .FilterTestUser(request.IncludeTest)
            .FilterAffiliate(request);

        return claimBonusResult;
    }

    private static string MapMpaWinLossGroupByTypeEnumFormat(MpaWinLossGroupByTypeEnum groupByType)
    {
        switch (groupByType)
        {
            case MpaWinLossGroupByTypeEnum.Date:
            case MpaWinLossGroupByTypeEnum.Month:
            case MpaWinLossGroupByTypeEnum.Brand:
            case MpaWinLossGroupByTypeEnum.DateDownload:
            case MpaWinLossGroupByTypeEnum.MonthDownload:
            case MpaWinLossGroupByTypeEnum.GameType:
            case MpaWinLossGroupByTypeEnum.TpCategory:
            case MpaWinLossGroupByTypeEnum.Player:
            case MpaWinLossGroupByTypeEnum.Currency:
            case MpaWinLossGroupByTypeEnum.Affiliate:
                return "%m/%d/%Y %H:00";

            //case MpaWinLossGroupByTypeEnum.GameType:
            //case MpaWinLossGroupByTypeEnum.TpCategory:
            //case MpaWinLossGroupByTypeEnum.Player:
            //case MpaWinLossGroupByTypeEnum.Currency:
            //case MpaWinLossGroupByTypeEnum.Affiliate:
            //    return "%Y-%m-%d";
            default:
                return string.Empty;
        }
    }

    #endregion

    private static void SummaryByTransactionSummaryType(MpaWinLossGroupByModel data,
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

    private static MpaWinLossGroupByModel GetSummary(IEnumerable<MpaWinlossRecordDto> records, int currencyId)
    {
        var r = records.ToList();
        var totalStakeSummary = r.Sum(x => x.Stake);
        var totalValidBetSummary = r.Sum(x => x.ValidBet);
        var totalWinLossSummary = r.Sum(x => x.WinLoss);
        var totalProgressiveWinSummary = r.Sum(x => x.ProgressiveWin);
        var totalProgressiveBetSummary = r.Sum(x => x.ProgressiveBet);
        var totalRowCount = r.Sum(x => x.RowCount);
        var totalBaCommission = r.Sum(x => x.BaCommission);
        var totalBaWinLossSummary = r.Sum(x => x.BaWinLoss);
        var totalWinLossSummaryWithoutBa = totalWinLossSummary - totalBaWinLossSummary;
        var ggr = r.Sum(x => x.GGR);
        var totalCommission = r.Sum(x => x.Revenue);

        return new MpaWinLossGroupByModel
        {
            CurrencyId = currencyId,
            TurnOver = totalStakeSummary,
            ValidTurnOver = totalValidBetSummary,
            Ggr = ggr,
            PlayerWinLoss = totalWinLossSummaryWithoutBa,
            BaPlayerWinLoss = totalBaWinLossSummary,
            ProgressiveWin = totalProgressiveWinSummary,
            ProgressiveBet = totalProgressiveBetSummary,
            Commission = totalCommission,
            BaCommission = totalBaCommission,
            BetCount = totalRowCount
        };
    }

    private MpaWinLossGroupByModel GetMpaGroupSummary(IEnumerable<MpaWinLossGroupByModel> groupBySummary,
        int currencyId,
        MpaWinLossGroupByTypeEnum groupByType)
    {

        var r = groupBySummary.ToList();
        var ggrSum = r.Sum(x => x.Ggr);
        var turnOverSum = r.Sum(x => x.TurnOver);
        var betCountSum = r.Sum(x => x.BetCount);
        var validTurnOverSum = r.Sum(x => x.ValidTurnOver);

        var result = new MpaWinLossGroupByModel
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
            Margin = validTurnOverSum != 0 ? ggrSum / validTurnOverSum : 0.00m,
            UniquePlayers = r.Sum(x => x.UniquePlayers)
        };

        if (MpaWinLossDefinition.SummaryTransaction.Contains(groupByType))
        {
            result.DepositCount = r.Sum(x => x.DepositCount);
            result.TotalDepositAmount = r.Sum(x => x.TotalDepositAmount);
            result.WithdrawalCount = r.Sum(x => x.WithdrawalCount);
            result.TotalWithdrawalAmount = r.Sum(x => x.TotalWithdrawalAmount);
            result.TotalBonus = r.Sum(x => x.TotalBonus);
        }

        return result;
    }

    /// <summary>
    /// 優化版本
    /// </summary>
    /// <param name="currencyId"></param>
    /// <param name="summaries"></param>
    /// <returns></returns>
    private static MpaWinLossGroupByModel GetSummary(int currencyId, SummariesModel summaries)
    {
        return new MpaWinLossGroupByModel
        {
            CurrencyId = currencyId,
            TurnOver = summaries.TotalStakeSummary,
            ValidTurnOver = summaries.TotalValidBetSummary,
            Ggr = summaries.GGR,
            PlayerWinLoss = summaries.TotalWinLossSummary - summaries.TotalBaWinLossSummary,
            BaPlayerWinLoss = summaries.TotalBaWinLossSummary,
            ProgressiveWin = summaries.TotalProgressiveWinSummary,
            ProgressiveBet = summaries.TotalProgressiveBetSummary,
            Commission = summaries.TotalCommission,
            BaCommission = summaries.TotalBaCommission,
            BetCount = summaries.TotalRowCount
        };
    }

    /// <summary>
    /// 優化版本
    /// </summary>
    /// <param name="currencyId"></param>
    /// <param name="summaries"></param>
    /// <param name="groupByType"></param>
    /// <returns></returns>
    private static MpaWinLossGroupByModel GetMpaGroupSummary(int currencyId, SummariesMpaModel summaries, bool IsSummaryTransaction)
    {
        var result = new MpaWinLossGroupByModel
        {
            CurrencyId = currencyId,
            TurnOver = summaries.TurnOver,
            ValidTurnOver = summaries.ValidTurnOver,
            Ggr = summaries.Ggr,
            PlayerWinLoss = summaries.PlayerWinLoss,
            BaPlayerWinLoss = summaries.BaPlayerWinLoss,
            ProgressiveWin = summaries.ProgressiveWin,
            ProgressiveBet = summaries.ProgressiveBet,
            Commission = summaries.Commission,
            BaCommission = summaries.BaCommission,
            BetCount = summaries.BetCount,
            AvgBetAmount = summaries.BetCount != 0 ? summaries.ValidTurnOver / summaries.BetCount : 0.00m,
            Margin = summaries.ValidTurnOver != 0 ? summaries.Ggr / summaries.ValidTurnOver : 0.00m,
            UniquePlayers = summaries.UniquePlayers,
        };

        if (IsSummaryTransaction)
        {
            result.DepositCount = summaries.DepositCount;
            result.TotalDepositAmount = summaries.TotalDepositAmount;
            result.WithdrawalCount = summaries.WithdrawalCount;
            result.TotalWithdrawalAmount = summaries.TotalWithdrawalAmount;
            result.TotalBonus = summaries.TotalBonus;
        }

        return result;
    }

    private static bool IsSummaryTransaction(MpaWinLossGroupByTypeEnum groupByType)
    => MpaWinLossDefinition.SummaryTransaction.Contains(groupByType) ? true : false;


    #region backup

    private static MpaWinLossGroupByModel GetSummaryBackup(IEnumerable<MpaWinlossRecordDto> records, int currencyId)
    {
        var r = records.ToList();
        var totalStakeSummary = r.Sum(x => x.Stake);
        var totalValidBetSummary = r.Sum(x => x.ValidBet);
        var totalWinLossSummary = r.Sum(x => x.WinLoss);
        var totalProgressiveWinSummary = r.Sum(x => x.ProgressiveWin);
        var totalProgressiveBetSummary = r.Sum(x => x.ProgressiveBet);
        var totalRowCount = r.Sum(x => x.RowCount);
        var totalBaCommission = r.Sum(x => x.BaValidBet);
        var totalBaWinLossSummary = r.Sum(x => x.BaWinLoss);
        var totalWinLossSummaryWithoutBa = totalWinLossSummary - totalBaWinLossSummary;
        var ggr = r.Sum(x => x.GGR);
        var totalCommission = r.Sum(x => x.Revenue);

        return new MpaWinLossGroupByModel
        {
            CurrencyId = currencyId,
            TurnOver = totalStakeSummary,
            ValidTurnOver = totalValidBetSummary,
            Ggr = ggr,
            PlayerWinLoss = totalWinLossSummaryWithoutBa,
            BaPlayerWinLoss = totalBaWinLossSummary,
            ProgressiveWin = totalProgressiveWinSummary,
            ProgressiveBet = totalProgressiveBetSummary,
            Commission = totalCommission,
            BaCommission = totalBaCommission,
            BetCount = totalRowCount
        };
    }

    private MpaWinLossGroupByModel GetMpaGroupSummaryBackup(IEnumerable<MpaWinLossGroupByModel> groupBySummary,
        int currencyId,
        MpaWinLossGroupByTypeEnum groupByType)
    {

        var r = groupBySummary.ToList();
        var ggrSum = r.Sum(x => x.Ggr);
        var turnOverSum = r.Sum(x => x.TurnOver);
        var betCountSum = r.Sum(x => x.BetCount);
        var validTurnOverSum = r.Sum(x => x.ValidTurnOver);

        var result = new MpaWinLossGroupByModel
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
            Margin = validTurnOverSum != 0 ? ggrSum / validTurnOverSum : 0.00m,
            UniquePlayers = r.Sum(x => x.UniquePlayers)
        };

        if (MpaWinLossDefinition.SummaryTransaction.Contains(groupByType))
        {
            result.DepositCount = r.Sum(x => x.DepositCount);
            result.TotalDepositAmount = r.Sum(x => x.TotalDepositAmount);
            result.WithdrawalCount = r.Sum(x => x.WithdrawalCount);
            result.TotalWithdrawalAmount = r.Sum(x => x.TotalWithdrawalAmount);
            result.TotalBonus = r.Sum(x => x.TotalBonus);
        }

        return result;
    }

    #endregion
}
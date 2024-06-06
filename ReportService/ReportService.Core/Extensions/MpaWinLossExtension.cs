using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;
using ReportService.Core.Models.MpaWinLoss;

namespace ReportService.Core.Extensions;

public static class MpaWinLossExtension
{
    /// <summary>
    /// 一般版本使用
    /// </summary>
    /// <param name="data"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public static IEnumerable<MpaTpBetRecordDto> Filter(this IEnumerable<MpaTpBetRecordDto> data, MpaWinLossRequest request)
    {
        var exceptTicketStatus = new List<int>();

        if (!request.NoDraw)
        {
            exceptTicketStatus.Add((int)TicketStatusEnum.Draw);
        }

        if (!request.IncludeFreeSpins)
        {
            exceptTicketStatus.Add((int)TicketStatusEnum.FreeBetWin);
        }

        if (exceptTicketStatus.Any())
        {
            data = data.Where(x => !exceptTicketStatus.Contains(x.TicketStatus));
        }

        return data
            .FilterAffiliate(request)
            .FilterTestUser(request.IncludeTest);
    }

    public static IEnumerable<T> FilterTestUser<T>(this IEnumerable<T> data, bool includeTest) where T : IFilterMpaWinLoss
        => includeTest ? data : data.Where(x => x.IsTest == 0);

    public static IEnumerable<T> FilterAffiliate<T>(this IEnumerable<T> data, MpaWinLossRequest request)
        where T : IFilterMpaWinLoss
        => request.GroupByType == MpaWinLossGroupByTypeEnum.Affiliate
           && (!request.AffiliateNames?.Any() ?? true) ? data.Where(x => !string.IsNullOrEmpty(x.AffiliateName)) : data;

    // BA用戶是指saba後台設定的特殊用戶，這些用戶的輸贏直接歸沙巴，沙巴會給平台傭金
    public static IEnumerable<MpaTpBetRecordDto> SetBAdata(this IEnumerable<MpaTpBetRecordDto> data)
    {
        // BA只計算SBSW Sport
        foreach (var item in data.Where(x =>
                     x.GameType == (int)MpaCommissionCustomizeGameTypeEnum.SBSW_Sports
                     && (x.SummaryType is (int)TpBetRecordSummaryTypeEnum.BA_SettledTime or (int)TpBetRecordSummaryTypeEnum.BA_TransTime)))
        {
            // 把BA資料區隔出來，避免影響一般資料計算
            item.BaStakeSummary = item.StakeSummary;
            item.BaValidBetSummary = item.ValidBetSummary;
            item.BaRevenueSummary = item.RevenueSummary;
            item.BaWinLossSummary = item.WinLossSummary;
            item.BaProgressiveBetSummary = item.ProgressiveBetSummary;
            item.BaProgressiveWinSummary = item.ProgressiveWinSummary;
            item.StakeSummary = 0;
            item.ValidBetSummary = 0;
            item.RevenueSummary = 0;
            item.WinLossSummary = 0;
            item.ProgressiveBetSummary = 0;
            item.ProgressiveWinSummary = 0;
        }
        return data;
    }

    // 用戶的輸贏直接歸9W，9W會給平台傭金
    public static IEnumerable<MpaTpBetRecordDto> SetCommissionByNineW(this IEnumerable<MpaTpBetRecordDto> data,
        MpaWinLossRequest request,
        string format,
        Func<DateTimeOffset, DateTimeOffset, string, MpaDateType, IEnumerable<Commission9WDto>> getCommission)
    {
        var commission = getCommission(request.StartTime, request.EndTime, format, request.DateType);

        switch (request.DateType)
        {
            // If filtered by Settled Time: Commission = - TpReport9W.commission
            case MpaDateType.SettledDate:
                {
                    foreach (var item in commission)
                    {
                        // 用commission抓第一筆符合9w/date/custId的資料，塞入傭金金額
                        var matchRecord = data.FirstOrDefault(r =>
                            r.TpId == (int)MpaCommissionCustomizeTpIdEnum.NineW
                            && r.Date == item.Date
                            && r.CustId == item.CustId);
                        if (matchRecord != null)
                        {
                            matchRecord.RevenueSummary = item.Commission;
                        }
                    }

                    return data;
                }
            // If filtered by Created Time: Commission = 0
            case MpaDateType.CreatedDate:
                {
                    foreach (var d in data.Where(x =>
                                 x.TpId == (int)MpaCommissionCustomizeTpIdEnum.NineW))
                    {
                        d.RevenueSummary = 0;
                    }

                    return data;
                }

            default:
                return data;
        }
    }

    /// <summary>
    /// 優化版本使用
    /// </summary>
    public static readonly Func<MpaWinlossRecordDto, object>[] SumSelectors = new Func<MpaWinlossRecordDto, object>[]
    {
        x => x.Stake,
        x => x.ValidBet,
        x => x.WinLoss,
        x => x.ProgressiveWin,
        x => x.ProgressiveBet,
        x => x.RowCount,
        x => x.BaValidBet,
        x => x.BaWinLoss,
        x => x.GGR,
        x => x.Revenue
    };

    /// <summary>
    /// 優化版本使用
    /// </summary>
    public static readonly Func<MpaWinLossGroupByModel, object>[] SumSelectorsTest = new Func<MpaWinLossGroupByModel, object>[]
    {
        x => x.TurnOver,
        x => x.ValidTurnOver,
        x => x.Ggr,
        x => x.PlayerWinLoss,
        x => x.BaPlayerWinLoss,
        x => x.ProgressiveWin,
        x => x.ProgressiveBet,
        x => x.Commission,
        x => x.BaCommission,
        x => x.BetCount,
        x => x.UniquePlayers,
        x => x.DepositCount,
        x => x.TotalDepositAmount,
        x => x.WithdrawalCount,
        x => x.TotalWithdrawalAmount,
        x => x.TotalBonus
    };

    /// <summary>
    /// 優化版本使用
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="source"></param>
    /// <param name="keySelector"></param>
    /// <param name="sumSelectors"></param>
    /// <returns></returns>
    public static IEnumerable<GroupedSummary<TKey>> GroupAndSummarize<TKey>(
    this IEnumerable<MpaWinlossRecordDto> source,
    Func<MpaWinlossRecordDto, TKey> keySelector)
    {
        var groupedData = source
            .GroupBy(keySelector)
            .AsParallel() //平行處理
            .Select(g => new GroupedSummary<TKey>
            {
                GroupKey = g.Key,
                Records = g,
                Summaries = new SummariesModel
                {
                    TotalStakeSummary = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[0](x))),
                    TotalValidBetSummary = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[1](x))),
                    TotalWinLossSummary = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[2](x))),
                    TotalProgressiveWinSummary = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[3](x))),
                    TotalProgressiveBetSummary = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[4](x))),
                    TotalRowCount = g.Sum(x => Convert.ToInt16(MpaWinLossExtension.SumSelectors[5](x))),
                    TotalBaCommission = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[6](x))),
                    TotalBaWinLossSummary = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[7](x))),
                    GGR = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[8](x))),
                    TotalCommission = g.Sum(x => Convert.ToDecimal(MpaWinLossExtension.SumSelectors[9](x))),
                }
            });

        return groupedData;
    }

    /// <summary>
    /// 優化版本使用
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="source"></param>
    /// <param name="keySelector"></param>
    /// <param name="sumSelectors"></param>
    /// <returns></returns>
    public static IEnumerable<GroupedSummaryTest<TKey>> GroupAndSummarizeTest<TKey>(
        this IEnumerable<MpaWinLossGroupByModel> source,
        Func<MpaWinLossGroupByModel, TKey> keySelector)
    {
        return source
            .GroupBy(keySelector)
            .AsParallel() //平行處理
            .Select(g => new GroupedSummaryTest<TKey>
            {
                GroupKey = g.Key,
                Records = g,
                Summaries = new SummariesMpaModel
                {
                    TurnOver = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[0](x))),
                    ValidTurnOver = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[1](x))),
                    Ggr = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[2](x))),
                    PlayerWinLoss = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[3](x))),
                    BaPlayerWinLoss = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[4](x))),
                    ProgressiveWin = g.Sum(x => Convert.ToInt16(SumSelectorsTest[5](x))),
                    ProgressiveBet = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[6](x))),
                    Commission = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[7](x))),
                    BaCommission = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[8](x))),
                    BetCount = g.Sum(x => Convert.ToInt16(SumSelectorsTest[9](x))),
                    UniquePlayers = g.Sum(x => Convert.ToInt16(SumSelectorsTest[10](x))),
                    DepositCount = g.Sum(x => Convert.ToInt16(SumSelectorsTest[11](x))),
                    TotalDepositAmount = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[12](x))),
                    WithdrawalCount = g.Sum(x => Convert.ToInt16(SumSelectorsTest[13](x))),
                    TotalWithdrawalAmount = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[14](x))),
                    TotalBonus = g.Sum(x => Convert.ToDecimal(SumSelectorsTest[15](x))),
                }
            });
    }
}
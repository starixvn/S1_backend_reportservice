using ReportService.Core.Models;
using ReportService.Core.Models.Enums;

namespace ReportService.Core.Extensions;

public static class MpaWinLossExtension
{
    public static IEnumerable<MpaTpBetRecordDto> Filter(this IEnumerable<MpaTpBetRecordDto> data, MpaWinLossRequest request)
    {
        //todo: 在sp做
        // 過濾 GameType、CurrencyId、CategoryId
        data = data.Where(r => request.TpGameTypes.Contains(r.GameType) &&
                               request.CurrencyIds.Contains(r.CurrencyId) && request.TpCategorys.Contains(r.CategoryId));

        //todo: 在sp做
        //過濾SummaryType
        if (request.DateType == MpaDateType.SettledDate)
        {
            //DateType == SettledDate時，篩選1跟3，3為BA資料
            data = data.Where(r => r.SummaryType == 1 || r.SummaryType == 3);
        }
        else if(request.DateType == MpaDateType.CreatedDate)
        {
            //DateType == CreatedDate，篩選2跟4，4為BA資料
            data = data.Where(r => r.SummaryType == 2 || r.SummaryType == 4);
        }

        if (!request.NoDraw)
        {
            //Win = 1,Lost = 2,Refund = 3,/*Reject, Refund, Void*/,Draw = 4,Related = 5,FreeBetWin = 6
            data = data.Where(x => x.TicketStatus != 4);
        }

        if (!request.IncludeFreeSpins)
        {
            //Win = 1,Lost = 2,Refund = 3,/*Reject, Refund, Void*/,Draw = 4,Related = 5,FreeBetWin = 6
            data = data.Where(x => x.TicketStatus != 6);
        }


        if (!request.IncludeTest)
        {
            //istest = 0, test member
            data = data.Where(x => x.IsTest == 0);
        }

        //todo: 在sp做
        // if (!string.IsNullOrEmpty(request.AffiliateAccounts))
        // {
        //     data = data.Where(x => x.AffiliateName == request.AffiliateAccount);
        // }

        return data;
    }
    
    // BA用戶是指saba後台設定的特殊用戶，這些用戶的輸贏直接歸沙巴，沙巴會給平台傭金
    public static IEnumerable<MpaTpBetRecordDto> SetBAdata(this IEnumerable<MpaTpBetRecordDto> data)
    {
        // BA只計算SBSW Sport
        foreach (var item in data.Where(x =>
                     x.GameType == (int) MpaCommissionCustomizeGameTypeEnum.SBSW_Sports
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
            item.ProgressiveBetSummary= 0;
            item.ProgressiveWinSummary= 0;
        }
        return data;
    }

    // 用戶的輸贏直接歸9W，9W會給平台傭金
    public static IEnumerable<MpaTpBetRecordDto> SetCommissionByNineW(this IEnumerable<MpaTpBetRecordDto> data,
        MpaWinLossRequest request,
        string format,
        Func<DateTimeOffset, DateTimeOffset, string, MpaDateType, IEnumerable<Commission9WDto>> getCommission)
    {
        var commission = getCommission(request.StarTime, request.EndTime, format, request.DateType);

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
}
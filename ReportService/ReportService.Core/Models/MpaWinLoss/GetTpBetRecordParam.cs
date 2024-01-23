using Newtonsoft.Json;
using ReportService.Core.Extensions;
using ReportService.Core.Models.Enums;

namespace ReportService.Core.Models.MpaWinLoss;

public class GetTpBetRecordParam : GetTransactionParam
{
    public GetTpBetRecordParam()
    { 
    }
    
    public GetTpBetRecordParam(MpaWinLossRequest req, string format, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        SiteIdsJson = JsonConvert.SerializeObject(req.SiteIds);
        StartTime = startTime.UtcDateTime;
        EndTime = endTime.UtcDateTime;
        DateTimeFormat = format;
        
        var summaryTypes = req.DateType switch
        {
            MpaDateType.SettledDate => new[]
            {
                (int) TpBetRecordSummaryTypeEnum.SettledTime, (int) TpBetRecordSummaryTypeEnum.BA_SettledTime
            },
            MpaDateType.CreatedDate => new[]
            {
                (int) TpBetRecordSummaryTypeEnum.TransTime, (int) TpBetRecordSummaryTypeEnum.BA_TransTime
            },
            _ => Array.Empty<int>()
        };

        SummaryTypesJson = CommonExtension.SerializeEnumerableEmptyReturnNull(summaryTypes);
        MemberTypesJson = CommonExtension.SerializeEnumerableEmptyReturnNull(req.MemberTypes);
        UserNamesJson = CommonExtension.SerializeEnumerableEmptyReturnNull(req.UserNames);
        GameTypesJson = CommonExtension.SerializeEnumerableEmptyReturnNull(req.TpGameTypes);
        CurrencyIdsJson = CommonExtension.SerializeEnumerableEmptyReturnNull(req.CurrencyIds);
        CategoryIdsJson = CommonExtension.SerializeEnumerableEmptyReturnNull(req.TpCategorys);
        AffiliateNamesJson = CommonExtension.SerializeEnumerableEmptyReturnNull(req.AffiliateNames);
    }
    
    public string? SummaryTypesJson { get; set; }
    public string? GameTypesJson { get; set; }
    public string? CategoryIdsJson { get; set; }
}
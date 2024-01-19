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
        MemberTypesJson = JsonConvert.SerializeObject(req.MemberTypes);
        UserNamesJson = JsonConvert.SerializeObject(req.UserNames);
        GameTypesJson = JsonConvert.SerializeObject(req.TpGameTypes);
        CurrencyIdsJson = JsonConvert.SerializeObject(req.CurrencyIds);
        CategoryIdsJson = JsonConvert.SerializeObject(req.TpCategorys);
        AffiliateNamesJson = JsonConvert.SerializeObject(req.AffiliateNames);
    }
    
    public string? SummaryTypesJson { get; set; }
    public string? GameTypesJson { get; set; }
    public string? CategoryIdsJson { get; set; }
}
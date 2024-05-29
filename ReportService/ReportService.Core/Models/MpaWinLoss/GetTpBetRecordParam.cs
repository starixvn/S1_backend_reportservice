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
        DateType = req.DateType;

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

        SummaryTypesJson = CommonExtension.SerializeEnumerableEmpty(summaryTypes);
        MemberTypesJson = CommonExtension.SerializeEnumerableEmpty(req.MemberTypes);
        UserNamesJson = CommonExtension.SerializeEnumerableEmpty(req.UserNames);
        GameTypesJson = CommonExtension.SerializeEnumerableEmpty(req.TpGameTypes);
        CurrencyIdsJson = CommonExtension.SerializeEnumerableEmpty(req.CurrencyIds);
        CategoryIdsJson = CommonExtension.SerializeEnumerableEmpty(req.TpCategorys);
        AffiliateNamesJson = CommonExtension.SerializeEnumerableEmpty(req.AffiliateNames);
    }

    public MpaDateType DateType { get; set; }
    public string SummaryTypesJson { get; set; }
    public string GameTypesJson { get; set; }
    public string CategoryIdsJson { get; set; }
}
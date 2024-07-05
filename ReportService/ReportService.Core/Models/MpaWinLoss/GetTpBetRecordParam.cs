using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using ReportService.Core.Extensions;
using ReportService.Core.Models.Enums;
using Ubiety.Dns.Core.Records.General;

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
        IncludeTestData = req.IncludeTest;

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
        ExceptTicketStatusesJson = CommonExtension.SerializeEnumerableEmpty(ExceptTicketStatusList(req.NoDraw, req.IncludeFreeSpins));
    }

    public GetTpBetRecordParam(MpaWinLossRequest req, string format)
    {
        SiteIdsJson = JsonConvert.SerializeObject(req.SiteIds);
        StartTime = req.StartTime.UtcDateTime;
        EndTime = req.EndTime.UtcDateTime;
        DateTimeFormat = format;
        DateType = req.DateType;
        IncludeTestData = req.IncludeTest;

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
        ExceptTicketStatusesJson = CommonExtension.SerializeEnumerableEmpty(ExceptTicketStatusList(req.NoDraw, req.IncludeFreeSpins));
    }


    private List<int> ExceptTicketStatusList(bool noDraw, bool includeFreeSpins)
    {
        var exceptTicketStatus = new List<int>() { (int)TicketStatusEnum.Refund }; //TicketStatus=3 不計算

        if (noDraw)
        {
            exceptTicketStatus.Add((int)TicketStatusEnum.Draw);
        }

        if (!includeFreeSpins)
        {
            exceptTicketStatus.Add((int)TicketStatusEnum.FreeBetWin);
        }
        return exceptTicketStatus;
    }

    public MpaDateType DateType { get; set; }
    public string SummaryTypesJson { get; set; }
    public string GameTypesJson { get; set; }
    public string CategoryIdsJson { get; set; }
    public bool IncludeTestData { get; set; }
    public string ExceptTicketStatusesJson { get; set; }
}
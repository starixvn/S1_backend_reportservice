using Newtonsoft.Json;
using ReportService.Core.Extensions;

namespace ReportService.Core.Models.MpaWinLoss;

public class GetTransactionParam
{
    public GetTransactionParam()
    { 
    }
    
    public GetTransactionParam(MpaWinLossRequest req, string format)
    {
        SiteIdsJson = JsonConvert.SerializeObject(req.SiteIds);
        StartTime = req.StartTime.UtcDateTime;
        EndTime = req.EndTime.UtcDateTime;
        DateTimeFormat = format;
        
        MemberTypesJson = CommonExtension.SerializeEnumerableEmpty(req.MemberTypes);
        UserNamesJson = CommonExtension.SerializeEnumerableEmpty(req.UserNames);
        CurrencyIdsJson = CommonExtension.SerializeEnumerableEmpty(req.CurrencyIds);
        AffiliateNamesJson = CommonExtension.SerializeEnumerableEmpty(req.AffiliateNames);
    }
    
    public string SiteIdsJson { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string DateTimeFormat { get; set; }
    public string MemberTypesJson { get; set; }
    public string UserNamesJson { get; set; }
    public string CurrencyIdsJson { get; set; }
    public string AffiliateNamesJson { get; set; }
}
using Newtonsoft.Json;
using ReportService.Core.Extensions;
using ReportService.Core.Models.Enums;

namespace ReportService.Core.Models;

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
        
        MemberTypesJson = JsonConvert.SerializeObject(req.MemberTypes);
        UserNamesJson = JsonConvert.SerializeObject(req.UserNames);
        CurrencyIdsJson = JsonConvert.SerializeObject(req.CurrencyIds);
        AffiliateNamesJson = JsonConvert.SerializeObject(req.AffiliateNames);
    }
    
    public string SiteIdsJson { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string DateTimeFormat { get; set; }
    public string? MemberTypesJson { get; set; }
    public string? UserNamesJson { get; set; }
    public string? CurrencyIdsJson { get; set; }
    public string? AffiliateNamesJson { get; set; }
}
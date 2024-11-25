using System.Data;

using Dapper;

using Newtonsoft.Json;

using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MarketingReport;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Service.BLL;

namespace ReportService.Service.DAL;

public class MarketingService
{
    private readonly MySqlClient _reportDbClient;
    private readonly ILogger<MarketingService> _logger;

    public MarketingService(IServiceProvider provider)
    {
        _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        _logger = provider.GetRequiredService<ILogger<MarketingService>>();
    }

    public async Task<MarketingReportResponse> GetMarketingMarketingDailyReport(MarketingReportRequest request)
    {
        var p = new DynamicParameters();
        p.Add("_SiteId", request.SiteId, dbType: DbType.Int32);
        p.Add("_StartTime", request.StartTime, dbType: DbType.DateTime);
        p.Add("_EndTime", request.EndTime, dbType: DbType.DateTime);
        var result = await _reportDbClient.QueryAsync<MarketingReportResponse>("Service_MarketingDailyReport_Export_Get", p, 60);
        return result.First();
    }

}
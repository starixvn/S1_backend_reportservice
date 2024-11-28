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
    private readonly MySqlClient _reportbackupDbClient;
    private readonly ILogger<MarketingService> _logger;

    public MarketingService(IServiceProvider provider)
    {
        _reportbackupDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.ReportBackupDB);
        _logger = provider.GetRequiredService<ILogger<MarketingService>>();
    }

    public async Task<MarketingReportResponse> GetMarketingMarketingDailyReport(MarketingReportRequest request)
    {
        var p = new DynamicParameters();
        p.Add("_SiteId", request.SiteId, dbType: DbType.Int32);
        p.Add("_StartTime", request.StartTime, dbType: DbType.DateTime);
        p.Add("_EndTime", request.EndTime, dbType: DbType.DateTime);
        var result = await _reportbackupDbClient.QueryAsync<MarketingReportResponse>("Service_MarketingDailyReport_Export_Get", p, 60);
        return result.First();
    }

}
using System.Data;
using Dapper;
using Newtonsoft.Json;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MpaWinLoss;

namespace ReportService.Service.DAL;

public class MpaWinLossService
{
    private readonly MySqlClient _reportDbClient;
    private readonly MySqlClient _reportCenterDbClient;
    private readonly ILogger<MpaWinLossService> _logger;

    public MpaWinLossService(IEnumerable<MySqlClient> sqlClients, ILogger<MpaWinLossService> logger)
    {
        _reportDbClient = sqlClients.First(p => p.DataBase == DataBase.Report);
        _reportCenterDbClient = sqlClients.First(p => p.DataBase == DataBase.ReportCenter);
        _logger = logger;
    }

    //一般版本
    public Task<IEnumerable<TResult>> GetTpBetRecord<TResult>(GetTpBetRecordParam input)
    {
        _logger.LogInformation(JsonConvert.SerializeObject(input));

        var p = new DynamicParameters();
        p.Add("_SiteIds", input.SiteIdsJson, dbType: DbType.String);
        p.Add("_StartTime", input.StartTime, dbType: DbType.DateTime);
        p.Add("_EndTime", input.EndTime, dbType: DbType.DateTime);
        p.Add("_Format", input.DateTimeFormat, dbType: DbType.String);
        p.Add("_MemberTypes", input.MemberTypesJson, dbType: DbType.String);
        p.Add("_UserNames", input.UserNamesJson, dbType: DbType.String);
        p.Add("_SummaryTypes", input.SummaryTypesJson, dbType: DbType.String);
        p.Add("_GameTypes", input.GameTypesJson, dbType: DbType.String);
        p.Add("_CurrencyIds", input.CurrencyIdsJson, dbType: DbType.String);
        p.Add("_CategoryIds", input.CategoryIdsJson, dbType: DbType.String);
        p.Add("_AffiliateNames", input.AffiliateNamesJson, dbType: DbType.String);

        return _reportDbClient.QueryAsync<TResult>("Service_TpBetRecordRelayGroupByDate_Get", p, 60);
    }

    //優化版本
    public Task<IEnumerable<TResult>> GetWinLossSummary<TResult>(GetTpBetRecordParam input)
    {
        _logger.LogInformation(JsonConvert.SerializeObject(input));

        var p = new DynamicParameters();
        p.Add("_SiteIds", input.SiteIdsJson, dbType: DbType.String);
        p.Add("_StartTime", input.StartTime, dbType: DbType.DateTime);
        p.Add("_EndTime", input.EndTime, dbType: DbType.DateTime);
        p.Add("_Format", input.DateTimeFormat, dbType: DbType.String);
        p.Add("_MemberTypes", input.MemberTypesJson, dbType: DbType.String);
        p.Add("_UserNames", input.UserNamesJson, dbType: DbType.String);
        p.Add("_DateType", input.DateType, dbType: DbType.Int32);
        p.Add("_GameTypes", input.GameTypesJson, dbType: DbType.String);
        p.Add("_CurrencyIds", input.CurrencyIdsJson, dbType: DbType.String);
        p.Add("_CategoryIds", input.CategoryIdsJson, dbType: DbType.String);
        p.Add("_AffiliateNames", input.AffiliateNamesJson, dbType: DbType.String);
        p.Add("_IncludeTestData", input.IncludeTestData, dbType: DbType.Boolean);
        p.Add("_ExceptTicketStatuses", input.ExceptTicketStatusesJson, dbType: DbType.String);

        return _reportCenterDbClient.QueryAsync<TResult>("Service_WinLossSummaryGroupByDate_Get", p, 60);
    }

    public Task<IEnumerable<TResult>> GetTransaction<TResult>(GetTransactionParam input)
    {
        _logger.LogInformation(JsonConvert.SerializeObject(input));

        var p = new DynamicParameters();
        p.Add("_SiteIds", input.SiteIdsJson, dbType: DbType.String);
        p.Add("_StartTime", input.StartTime, dbType: DbType.DateTime);
        p.Add("_EndTime", input.EndTime, dbType: DbType.DateTime);
        p.Add("_Format", input.DateTimeFormat, dbType: DbType.String);
        p.Add("_MemberTypes", input.MemberTypesJson, dbType: DbType.String);
        p.Add("_UserNames", input.UserNamesJson, dbType: DbType.String);
        p.Add("_CurrencyIds", input.CurrencyIdsJson, dbType: DbType.String);
        p.Add("_AffiliateNames", input.AffiliateNamesJson, dbType: DbType.String);

        return _reportDbClient.QueryAsync<TResult>("Service_TransactionRelayGroupByDate_Get", p);
    }

    public Task<IEnumerable<TResult>> GetClaimBonus<TResult>(GetTransactionParam input)
    {
        _logger.LogInformation(JsonConvert.SerializeObject(input));

        var p = new DynamicParameters();
        p.Add("_SiteIds", input.SiteIdsJson, dbType: DbType.String);
        p.Add("_StartTime", input.StartTime, dbType: DbType.DateTime);
        p.Add("_EndTime", input.EndTime, dbType: DbType.DateTime);
        p.Add("_Format", input.DateTimeFormat, dbType: DbType.String);
        p.Add("_MemberTypes", input.MemberTypesJson, dbType: DbType.String);
        p.Add("_UserNames", input.UserNamesJson, dbType: DbType.String);
        p.Add("_CurrencyIds", input.CurrencyIdsJson, dbType: DbType.String);
        p.Add("_AffiliateNames", input.AffiliateNamesJson, dbType: DbType.String);

        return _reportDbClient.QueryAsync<TResult>("Service_MpaWinLossBonus_Get", p);
    }

    public IEnumerable<Commission9WDto> Get9WCommission(DateTimeOffset starTime, DateTimeOffset endTime, string format,
        MpaDateType mpaDateType = MpaDateType.SettledDate)
    {
        var p = new DynamicParameters();
        p.Add("_StartTime", starTime.ToUniversalTime().DateTime, dbType: DbType.DateTime);
        p.Add("_EndTime", endTime.ToUniversalTime().DateTime, dbType: DbType.DateTime);
        p.Add("_Format", format, dbType: DbType.String);
        p.Add("_DateType", (int)mpaDateType, dbType: DbType.Int16);

        return _reportDbClient.Query<Commission9WDto>("Admin_9WCommission_Get", p);
    }

    public async Task<Dictionary<(int, int), string>> GetAllCustVipLevelAsync()
    {
        var vipLevels = await _reportDbClient.QueryAsync<(int CustId, int SiteId, int VipLevel, string LevelName)>("Service_CustVipLevel_GetAll");

        return vipLevels.ToDictionary(x => (x.CustId, x.SiteId), x => x.LevelName);
    }
}
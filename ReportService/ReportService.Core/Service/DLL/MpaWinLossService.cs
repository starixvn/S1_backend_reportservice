using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MpaWinLoss;

namespace ReportService.Core.Service.DLL;

public class MpaWinLossService
{
    private readonly MySqlClient _reportDbClient;
    private readonly ILogger<MpaWinLossService> _logger;

    public MpaWinLossService(IEnumerable<MySqlClient> sqlClients, ILogger<MpaWinLossService> logger)
    {
        _reportDbClient = sqlClients.First(p => p.DataBase == DataBase.Report);
        _logger = logger;
    }
    
    public Task<IEnumerable<TResult>> GetTpBetRecord<TResult>(GetTpBetRecordParam input)
    {
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
        
        return _reportDbClient.QueryAsync<TResult>("Service_TpBetRecordRelayGroupByDate_Get", p);
    }
    
    public Task<IEnumerable<TResult>> GetTransaction<TResult>(GetTransactionParam input)
    {
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
}
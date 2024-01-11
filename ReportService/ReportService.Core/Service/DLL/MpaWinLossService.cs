using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;

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
    
    public Task<IEnumerable<TResult>> GetTpBetRecord<TResult>(IEnumerable<int> siteIds,
        DateTimeOffset starTime,
        DateTimeOffset endTime,
        string format,
        IEnumerable<MemberTypeEnum> memberTypes,
        IEnumerable<string> userNames,
        MpaDateType mpaDateType)
    {
        var p = new DynamicParameters();
        p.Add("_SiteIds", JsonConvert.SerializeObject(siteIds), dbType: DbType.String);
        p.Add("_StartTime", starTime.ToUniversalTime().DateTime, dbType: DbType.DateTime);
        p.Add("_EndTime", endTime.ToUniversalTime().DateTime, dbType: DbType.DateTime);
        p.Add("_Format", format, dbType: DbType.String);
        p.Add("_MemberTypeJson", JsonConvert.SerializeObject(memberTypes), dbType: DbType.String);
        p.Add("_UserNames", JsonConvert.SerializeObject(userNames), dbType: DbType.String);

        
        //todo: 調整新的 SP for service，要整理他們需要的filter
        var data = _reportDbClient.QueryAsync<TResult>("Admin_TpBetRecordRelayGroupByDate_Get_V6", p);

        return data;
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
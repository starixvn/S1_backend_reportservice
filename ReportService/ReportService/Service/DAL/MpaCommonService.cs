using System.Data;
using Dapper;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Models;

namespace ReportService.Service.DAL;

public class MpaCommonService
{
    private readonly MySqlClient _reportDbClient;
    private readonly ILogger<MpaCommonService> _logger;

    public MpaCommonService(IServiceProvider provider)
    {
        _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        _logger = provider.GetRequiredService<ILogger<MpaCommonService>>();
    }

    public Task<IEnumerable<MpaRateDto>> GetExchangeRate(DateTimeOffset starTime, DateTimeOffset endTime, ExchangeRateTypeEnum rateType)
    {
        var p = new DynamicParameters();
        p.Add("_RateDateStart", starTime.Date.AddDays(-1), dbType: DbType.DateTime);
        p.Add("_RateDateEnd", endTime.Date.AddMonths(1).AddDays(3), dbType: DbType.DateTime);
        p.Add("_RateType", (int)rateType, dbType: DbType.Int32);

        return _reportDbClient.QueryAsync<MpaRateDto>("Admin_ExchangeRate_Get_V2", p);
    }


    public Task<IEnumerable<TpGameTypeOptionDto>> GetTpGameTypeOption()
    {
        return _reportDbClient.QueryAsync<TpGameTypeOptionDto>("Common_TpGameType_Get");
    }

    public Task<IEnumerable<TpCategoryDto>> GetTpCategory()
    {
        return _reportDbClient.QueryAsync<TpCategoryDto>("Admin_TPCategory_Get");
    }
}
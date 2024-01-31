using ReportService.Core.Library;
using ReportService.Core.Models;
using ReportService.Models;
using ReportService.Service.DAL;

namespace ReportService.Service.BLL;

public class MpaCommonBLL
{
    private readonly MpaCommonService _mpaCommonService;
    private readonly ICacheHelper _redisHelper;
    private readonly ILogger<MpaWinLossBLL> _logger;
    private readonly IReadOnlyDictionary<int, string> _dicServerInfo;
    private readonly IReadOnlyDictionary<int, string> _dicCurrencyInfo;
   
    public MpaCommonBLL(IServiceProvider provider)
    {
        _mpaCommonService = provider.GetRequiredService<MpaCommonService>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<MpaWinLossBLL>>();
        _dicServerInfo = provider.GetRequiredService<IEnumerable<ServerInfo>>()
            .ToDictionary(x => x.SiteId, x => x.SiteName);
        _dicCurrencyInfo = provider.GetRequiredService<IEnumerable<CurrencyInfo>>()
            .ToDictionary(x => x.CurrencyId, x => x.IsoCode);
    }
    
    public async Task<Dictionary<string, decimal>> GetMapRate(DateTimeOffset starTime, DateTimeOffset endTime, int baseCurrencyId)
    {
        return (await GetMpaExchangeRate(starTime, endTime, baseCurrencyId))
            .Where(x => x.TargetCurrencyId == baseCurrencyId)
            .ToDictionary(k =>
                $"{k.RateDate.ToString("yyyy-MM-dd")}{k.CurrencyId}{k.TargetCurrencyId}", v => v.Rate);
    }
   
    private async Task<IEnumerable<MpaRateDto>> GetMpaExchangeRate(DateTimeOffset starTime, DateTimeOffset endTime, int baseCurrencyId = 0)
    {
        var redisKey = $"MpaExchangeRate:{starTime:yyyy-MM-dd HH:mm:ss}:{endTime:yyyy-MM-dd HH:mm:ss}:{baseCurrencyId}";

        if (baseCurrencyId == 0)
        {
            var exchangeRate = await _redisHelper.TryGetOrCreateAsync(redisKey,
                TimeSpan.FromMinutes(10),
                () => _mpaCommonService.GetExchangeRate(starTime, endTime));

            return exchangeRate;
        }
        else
        {
            var exchangeRate = await _redisHelper.TryGetOrCreateAsync(redisKey,
                TimeSpan.FromMinutes(10),
                async () => (await _mpaCommonService.GetExchangeRate(starTime, endTime))
                    .Where(x => x.TargetCurrencyId == baseCurrencyId));

            return exchangeRate;
        }
    }
    
    public async Task<Dictionary<int, string>> GetTpGameTypeMapper()
    {
        const string redisKey = "MpaTpGameTypeMapper";
        var data = await _redisHelper.TryGetOrCreateAsync(redisKey,
            TimeSpan.FromHours(12),
            () => _mpaCommonService.GetTpGameTypeOption());

        return data.ToDictionary(x => x.Type, v => v.TypeName);
    }


    public async Task<Dictionary<int, string>> GetTpCategoryMapper()
    {
        const string redisKey = "MpaTpCategoryMapper";
        var data = await _redisHelper.TryGetOrCreateAsync(redisKey,
            TimeSpan.FromHours(12),
            () => _mpaCommonService.GetTpCategory());

        return data.ToDictionary(x => x.CategoryId, v => v.CategoryName);
    }

    public string ToSiteName(int siteId)
        => _dicServerInfo[siteId];
    
    public string ToCurrencyIsoCode(int currencyId)
        => _dicCurrencyInfo[currencyId];
}
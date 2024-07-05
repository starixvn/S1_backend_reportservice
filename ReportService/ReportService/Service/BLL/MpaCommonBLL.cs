using ReportService.Core.Extensions;
using ReportService.Core.Library;
using ReportService.Core.Models;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Extensions;
using ReportService.Models;
using ReportService.Service.DAL;

namespace ReportService.Service.BLL;

public class MpaCommonBLL
{
    private readonly MpaCommonService _mpaCommonService;
    private readonly ICacheHelper _redisHelper;
    private readonly IReadOnlyDictionary<int, string> _dicServerInfo;
    private readonly IReadOnlyDictionary<int, string> _dicCurrencyInfo;
    private readonly IReadOnlyDictionary<int, string> _dicKCurrencyInfo;

    public MpaCommonBLL(IServiceProvider provider)
    {
        _mpaCommonService = provider.GetRequiredService<MpaCommonService>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _dicServerInfo = provider.GetRequiredService<IEnumerable<ServerInfo>>()
            .ToDictionary(x => x.SiteId, x => x.SiteName);
        _dicCurrencyInfo = provider.GetRequiredService<IEnumerable<CurrencyInfo>>()
            .ToDictionary(x => x.CurrencyId, x => x.IsoCode);
        _dicKCurrencyInfo = new Dictionary<int, string>() { { 15, "IDR" }, {51, "VND" } }; //轉圜成OriginalCurrency的時候，這些幣別要用1000分位去顯示
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

    public IEnumerable<MpaWinLossGroupByModel> ConvertToOriginalCurrency(IEnumerable<MpaWinLossGroupByModel> source)
    {
        var kCurrencyResult = source
            .Where(x => _dicKCurrencyInfo.ContainsKey(x.CurrencyId))
             .Select(r =>
             {
                 r.ExchangeMpaWinLossGroupBySummary(0.001m);

                 return r;
             });

        return source.Where(x => !_dicKCurrencyInfo.ContainsKey(x.CurrencyId)).Concat(kCurrencyResult);
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportService.Core.Library;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;
using ReportService.Core.Service.DLL;

namespace ReportService.Core.Service.BLL;

public class MpaCommonBLL
{
    private readonly MpaCommonService _mpaCommonService;
    private readonly ICacheHelper _redisHelper;
    private readonly ILogger<MpaWinLossBLL> _logger;
   
    public MpaCommonBLL(IServiceProvider provider)
    {
        _mpaCommonService = provider.GetRequiredService<MpaCommonService>();
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<MpaWinLossBLL>>();
    
    }
    
    public async Task<Dictionary<string, decimal>> GetMapRate(DateTimeOffset starTime, DateTimeOffset endTime, CurrencyEnum baseCurrency)
    {
        var baseCurrencyId = (int) baseCurrency;
        
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
    
    /*public decimal MapperExchangeRate(IEnumerable<MpaRateDto> source, DateTime date, int currencyId, int baseCurrencyId)
    {
        //是否是當天
        if (date == DateTime.Today)
        {
            return source
                .FirstOrDefault(r => r.RateDate.ToString("yyyy-MM-dd") == date.ToString("yyyy-MM-dd")
                                     && r.CurrencyId == currencyId
                                     && r.TargetCurrencyId == baseCurrencyId)!.Rate;
        }

        //是否是當月
        if (date.ToString("yyyy-MM") == DateTime.Now.ToString("yyyy-MM"))
        {
            return source
                .Where(r => r.RateDate.ToString("yyyy-MM") == DateTime.Now.ToString("yyyy-MM")
                            && r.CurrencyId == currencyId
                            && r.TargetCurrencyId == baseCurrencyId).MaxBy(r => r.RateDate)!.Rate;
        }


        //抓同年同月的最後一筆匯率
        var lastDay = date.AddMonths(1).AddDays(-date.AddMonths(1).Day).ToString("yyyy-MM-dd");
        return source.FirstOrDefault(r => r.RateDate.ToString("yyyy-MM-dd") == lastDay
                                          && r.CurrencyId == currencyId
                                          && r.TargetCurrencyId == baseCurrencyId)!.Rate;
    }*/
    
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
}
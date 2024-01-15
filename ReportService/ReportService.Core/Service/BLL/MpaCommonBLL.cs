using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportService.Core.Library;
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
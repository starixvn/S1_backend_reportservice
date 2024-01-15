using Microsoft.Extensions.Logging;
using ReportService.Core.Models;
using ReportService.Core.Models.Enums;

namespace ReportService.Core.Service.DLL;

public class MpaCommonService
{
    private readonly MySqlClient _reportDbClient;
    private readonly ILogger<MpaCommonService> _logger;

    public MpaCommonService(IEnumerable<MySqlClient> sqlClients, ILogger<MpaCommonService> logger)
    {
        _reportDbClient = sqlClients.First(p => p.DataBase == DataBase.Report);
        _logger = logger;
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
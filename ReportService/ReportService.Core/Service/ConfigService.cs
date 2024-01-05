using Microsoft.Extensions.DependencyInjection;
using ReportService.Core.DataLayers;
using ReportService.Core.Models.Enums;

namespace ReportService.Core.Service;

public class ConfigService
{
    private readonly MySqlClient _reportDbClient;

    public ConfigService(IServiceProvider provider)
    {
        _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
    }

    public IEnumerable<T> Query<T>(string sql, object parameters = null)
    {
        return _reportDbClient.Query<T>(sql, parameters);
    }
}
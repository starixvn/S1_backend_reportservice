using ReportService.Core.DataLayers;
using ReportService.Core.Library;
using ReportService.Core.Models.Enums;
using ReportService.Core.Service;

namespace ReportService.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
        
        return services
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger, DataBase.Main))
            .AddSingleton(param => new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger, DataBase.Report));
    }
    
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSingleton<ICacheHelper, RedisCacheHelper>()
            .AddSingleton<ConfigService>();
    }
}
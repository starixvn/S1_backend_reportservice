using Elastic.Apm.StackExchange.Redis;
using ReportService.Core.Library;
using ReportService.Core.Models.Enums;
using ReportService.ExceptionHandling;
using ReportService.Service.DAL;
using StackExchange.Redis;

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
        var redisConnection = ConnectionMultiplexer.Connect(configuration.GetValue<string>("RedisConnection"));
        redisConnection.UseElasticApm();
        services.AddSingleton<IConnectionMultiplexer>(redisConnection);
        
        // DAL
        var dalAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var item in dalAssemblies)
        {
            var list = item.DefinedTypes.Where(t => t.IsClass
                                                    && t.Namespace == "ReportService.Service.DAL"
                                                    && t.Name.Contains("Service")).ToList();

            list.ForEach((type) =>
            {
                services.AddSingleton(type, type);
            });
        }

        // BLL
        var bllAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var item in bllAssemblies)
        {
            var list = item.DefinedTypes.Where(t => t.IsClass
                                                    && t.Namespace == "ReportService.Service.BLL"
                                                    && t.Name.Contains("BLL")).ToList();

            list.ForEach((type) =>
            {
                services.AddSingleton(type, type);
            });
        }
        
        // Filters
        services.AddMvc(config =>
        {
            config.Filters.Add<ExceptionFilter>();
        });
        
        return services
            .AddSingleton<ICacheHelper, RedisCacheHelper>();
    }
}
using Elastic.Apm.StackExchange.Redis;
using ReportService.Core.Library;
using ReportService.Core.Models.Enums;
using ReportService.ExceptionHandling;
using ReportService.Models;
using ReportService.Service.BLL;
using ReportService.Service.DAL;
using StackExchange.Redis;

namespace ReportService.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<MySqlClient>>();
        var serviceType = configuration.GetValue<string>("ServiceType");

        var reportDBConn = configuration.GetConnectionString($"{serviceType}_ReportDB");
        var reportBackupDB1 = configuration.GetConnectionString($"{serviceType}_ReportBackupDB1");
        var reportBackupDB2 = configuration.GetConnectionString($"{serviceType}_ReportBackupDB2");

        //檢查是否有BackupDB連線字串
        if (!string.IsNullOrEmpty(reportBackupDB1) && !string.IsNullOrEmpty(reportBackupDB2))
        {
            // 設定主要的ReportDB與BackupDB1
            services.AddSingleton(new MySqlClient(DataBase.Report, logger, reportDBConn, reportBackupDB1));
            // 設定Backup，使用BackupDB1與BackupDB2
            services.AddSingleton(new MySqlClient(DataBase.ReportBackupDB, logger, reportBackupDB1, reportBackupDB2));
        }
        else
        {
            services.AddSingleton(new MySqlClient(DataBase.Report, logger, reportDBConn));
        }


        return services;
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
        
        services.AddOptionListByDb<ServerInfo>("ServerInfoList_Get");
        services.AddOptionListByDb<CurrencyInfo>("Common_Currency_Get");
        services.AddOptionListByDb<Config>("SiteConfig_Get", new { ConfigGroup = "PaymentService" });
        services.AddSiteConfig();
        services.AddSingleton<ConfigBLL>();
        
        // Filters
        services.AddMvc(config =>
        {
            config.Filters.Add<ExceptionFilter>();
        });
        
        return services
            .AddSingleton<ICacheHelper, RedisCacheHelper>();
            //.AddHostedService<GarbageCollectCheckWorker>();
    }
    
    private static IServiceCollection AddOptionListByDb<T>(this IServiceCollection services, string sql, object parameters = null)
    {
        var provider = services.BuildServiceProvider();
        var configService = provider.GetRequiredService<ConfigService>();
        var option = configService.Query<T>(sql, parameters);
        services.AddSingleton(typeof(IEnumerable<T>), p => option);
        
        return services;
    }
    
    private static IServiceCollection AddSiteConfig(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        // 準備ServerInfo
        var serverInfoList = new List<ServerInfo> { new() { SiteId = 0, SiteName = "default" } };
        serverInfoList.AddRange(provider.GetRequiredService<IEnumerable<ServerInfo>>());

        // 準備Config
        var siteConfigGroup = provider.GetRequiredService<IEnumerable<Config>>().GroupBy(i => i.SiteId);

        foreach (var serverInfo in serverInfoList)
        {
            //設定SiteConfig
            var siteConfig = siteConfigGroup
                .FirstOrDefault(i => i.Key == serverInfo.SiteId) ?? siteConfigGroup.First(i => i.Key == 0);
            var config = siteConfig.ToDictionary(i => i.ConfigKey, i => i.ConfigValue);
            services.AddSingleton(new SiteConfig(serverInfo, config));
        }

        return services;
    }
}
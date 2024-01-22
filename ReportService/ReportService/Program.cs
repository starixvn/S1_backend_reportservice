using Elastic.Apm.SerilogEnricher;
using Newtonsoft.Json;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Extensions;
using ReportService.Service.BLL;
using Serilog;
using Serilog.Exceptions;
//
// var builder = WebApplication.CreateBuilder(args);
//
// var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
// remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
// builder.Configuration.AddRemoteJsonConfig(remoteUrls);
//
// var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
// var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();
// builder.Host.UseSerilog((context, Configuration) =>
// {
//     Configuration
//         .ReadFrom.Configuration(builder.Configuration)
//         .Enrich.WithElasticApmCorrelationInfo()
//         .Enrich.FromLogContext()
//         .Filter.ByExcluding(c =>
//             c.Properties.Any(p =>
//                 excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
//                 || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
//         .Enrich.WithExceptionDetails();
// });
//
// builder.Services
//     .AddDatabaseConnection(builder.Configuration)
//     .RegisterServices(builder.Configuration);
//
// JsonConvert.DefaultSettings = () => new JsonSerializerSettings
// {
//     NullValueHandling = NullValueHandling.Ignore
// };
//
// var app = builder.Build();
// app.MapControllers();
// app.UseRouting();
// app.UseRequestLogMiddleware();
// app.UseResponseLogMiddleware();
// app.Run();


namespace ReportService
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);
                var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
                remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
                builder.Configuration.AddRemoteJsonConfig(remoteUrls);

                builder.Host.UseSerilog((context, Configuration) =>
                {
                    Configuration
                        .ReadFrom.Configuration(builder.Configuration)
                        .Enrich.FromLogContext();
                });

                var services = builder.Services
                    .AddDatabaseConnection(builder.Configuration)
                    .RegisterServices(builder.Configuration);

                var service = services.BuildServiceProvider()
                    .GetRequiredService<MpaWinLossBLL>();

                var result = service.GetMpaWinLossGroupByDateReport(new MpaWinLossRequest
                {
                    SiteIds = new [] {1, 2, 3},
                    CurrencyIds = new []
                    {
                        CurrencyEnum.MYR,
                        CurrencyEnum.THB,
                        CurrencyEnum.IDR,
                        CurrencyEnum.CNY,
                        CurrencyEnum.VND,
                        CurrencyEnum.INR,
                        CurrencyEnum.USD,
                        CurrencyEnum.BDT,
                        CurrencyEnum.KHUSD
                    },
                    StartTime = DateTimeOffset.Parse("2024-01-01T00:00:00+08:00"),
                    EndTime = DateTimeOffset.Parse("2024-01-01T23:59:59+08:00"),
                    
                }, MpaWinLossGroupByTypeEnum.Date);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
            
            Console.WriteLine("=== END ===");
            Console.ReadLine();
        }
    }
}
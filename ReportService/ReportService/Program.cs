using Elastic.Apm.SerilogEnricher;
using Newtonsoft.Json;
using ReportService.Core.Models.Enums;
using ReportService.Core.Models.MpaWinLoss;
using ReportService.Extensions;
using ReportService.Service.BLL;
using Serilog;
using Serilog.Exceptions;

var builder = WebApplication.CreateBuilder(args);

var remoteUrls = builder.Configuration.GetSection("ConfigService").Get<string[]>();
remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();
builder.Host.UseSerilog((context, Configuration) =>
{
    Configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.WithElasticApmCorrelationInfo()
        .Enrich.FromLogContext()
        .Filter.ByExcluding(c =>
            c.Properties.Any(p =>
                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
        .Enrich.WithExceptionDetails();
});

builder.Services
    .AddDatabaseConnection(builder.Configuration)
    .RegisterServices(builder.Configuration)
    .AddControllers();

JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    NullValueHandling = NullValueHandling.Ignore
};

var app = builder.Build();
app.MapControllers();
app.UseRouting();
app.UseRequestLogMiddleware();
app.UseResponseLogMiddleware();
app.Run();


/*namespace ReportService
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
                    TpCategorys = new []{1,2,3,4,6,7,8,9,10,11,12},
                    TpGameTypes = new []{49,51,69,77,103,7,11,13,14,15,17,29,53,57,64,65,67,68,75,76,78,82,86,95,96,98,99,100,104,105,106,107,108,111,112,6,8,9,10,16,18,28,32,37,45,58,63,79,84,93,94,101,31,44,56,66,85,35,36,54,55,70,73,74,88,89,90,91,110,71,72,81,42,60,109,87,47,50,52,61,97,102},
                    UserNames = Array.Empty<string>(),
                    AffiliateNames = Array.Empty<string>(),
                    OrderBy = false,
                    OrderByDirection = 0,
                    OrderByProperty = string.Empty,
                    DateType = MpaDateType.SettledDate,
                    DisplayGroupBreakdownInOriginalCurrency = false,
                    IncludeTest = true,
                    MemberTypes = new [] {MemberTypeEnum.Affiliate, MemberTypeEnum.Normal},
                    NoDraw = true,
                    BaseCurrencyId = CurrencyEnum.MYR,
                    IncludeFreeSpins = true
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
}*/
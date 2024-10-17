using Newtonsoft.Json;
using ReportService.Extensions;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry();

var remoteUrls = GetRemoteUrls();
builder.Configuration.AddJsonFile("appsettings.json",
        optional: true,
        reloadOnChange: false);
//remoteUrls = builder.Configuration.CheckLocalConfig(builder.Environment, args, remoteUrls);
builder.Configuration.AddRemoteJsonConfig(remoteUrls);

var excludesMessage = builder.Configuration.GetSection("ExcludesMessage").Get<string[]>();
var excludesApiEndpoints = builder.Configuration.GetSection("ExcludesApiEndpoint").Get<string[]>();

Console.WriteLine(builder.Configuration.GetValue<string>("ENV"));
Console.WriteLine(builder.Configuration.GetValue<string>("Sentry:Dsn"));

//builder.Host.UseSerilog((context, Configuration) =>
//{
//    Configuration
//        .ReadFrom.Configuration(builder.Configuration)
//        .Enrich.FromLogContext()
//        .Filter.ByExcluding(c =>
//            c.Properties.Any(p =>
//                excludesApiEndpoints.Any(item => p.Value.ToString().Contains(item))
//                || excludesMessage.Any(item => c.MessageTemplate.ToString().Contains(item))))
//        .Enrich.WithExceptionDetails();

//    OverrideLoggerWriteToLFile(Configuration);
//});

builder.Services
    .AddDatabaseConnection(builder.Configuration)
    .RegisterServices(builder.Configuration)
    .AddControllers()
    .AddNewtonsoftJson(op =>
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
    });
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapControllers();
app.UseRouting();
app.UseRequestLogMiddleware();
app.UseResponseLogMiddleware();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/health", async context =>
    {
        await context.Response.WriteAsync($"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}");
    });
});
app.MapHealthChecks("/");

SentrySdk.CaptureMessage($"Started result Report Service at {DateTime.Now.ToString()}");


app.Run();

return;

void OverrideLoggerWriteToLFile(LoggerConfiguration configuration)
{
    const string cashSiteBrand = "Nova";

    var brand = Environment.GetEnvironmentVariable("Brand") ?? string.Empty;
    if (brand != cashSiteBrand) return;

    var logFilePath = $"/report/report-{DateTime.Now:yyyyMMddHHmmssfff}.log";
    configuration
        .WriteTo.File(new JsonFormatter(),
            logFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 60,
            fileSizeLimitBytes: 10485760,
            rollOnFileSizeLimit: true);
}

string[] GetRemoteUrls()
{
    // 如果有 Runtime Environment Variable 優先使用
    // 沒有則使用預設 AppSetting
    //return Environment.GetEnvironmentVariable("ConfigServiceUrl") is null
    //    ? builder.Configuration.GetSection("ConfigService").Get<string[]>()
    //    : new List<string>() { Environment.GetEnvironmentVariable("ConfigServiceUrl") }.ToArray();

    return builder.Configuration.GetSection("ConfigService").Get<string[]>();
}


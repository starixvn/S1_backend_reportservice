using Elastic.Apm.SerilogEnricher;
using Newtonsoft.Json;
using ReportService.Extensions;
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
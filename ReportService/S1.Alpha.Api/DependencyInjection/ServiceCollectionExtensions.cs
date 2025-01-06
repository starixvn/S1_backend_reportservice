using MySql.Data.MySqlClient;
using S1.Alpha.Api.Services;
using System.Text.Json;

namespace S1.Alpha.Api.DependencyInjection;

public class RemoteConfigs
{
    public Dictionary<string, string> connectionStrings { get; set; } = default!;
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlClient(this IServiceCollection services, IConfiguration configuration)
    {
        using var client = new HttpClient();
        var remoteUrl = configuration.GetSection("ConfigService").Get<string>();
        var result = client.GetAsync(remoteUrl).Result.Content.ReadAsStreamAsync().Result;
        var remoteConfigs = JsonSerializer.Deserialize<RemoteConfigs>(result);
        var connectionString = remoteConfigs?.connectionStrings["connectionString"];
        services.AddTransient(x => new MySqlConnection(connectionString));

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<IUserService, UserService>();

        return services;
    }
}

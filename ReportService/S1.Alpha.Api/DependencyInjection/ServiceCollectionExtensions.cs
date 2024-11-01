using MySql.Data.MySqlClient;
using S1.Alpha.Api.Services;

namespace S1.Alpha.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlClient(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DbConnectionString"];
        services.AddTransient(x => new MySqlConnection(connectionString));

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<IUserService, UserService>();

        return services;
    }
}

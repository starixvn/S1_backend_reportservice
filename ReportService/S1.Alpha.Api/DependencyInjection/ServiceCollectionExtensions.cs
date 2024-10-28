using MySql.Data.MySqlClient;

namespace S1.Alpha.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlClient(this IServiceCollection services)
    {
       //server=10.18.200.163;port=3361;user=app_srv;database=Siebog;password=1234aa;SslMode=None;AllowPublicKeyRetrieval=true;
        services.AddTransient(x => new MySqlConnection("server=10.18.200.163;port=3361;user=nova88;database=Siebog;password=Aa12345678!;SslMode=None;AllowPublicKeyRetrieval=true"));

        return services;
    }
}

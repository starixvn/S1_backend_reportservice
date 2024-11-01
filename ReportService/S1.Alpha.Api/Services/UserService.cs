using Dapper;
using MySql.Data.MySqlClient;
using S1.Alpha.Api.ViewModels;
using System.Data;

namespace S1.Alpha.Api.Services;

public interface IUserService
{
    ValueTask<bool> CheckExistsUser(string username, string referenceId);
}

public class UserService : IUserService
{
    private readonly MySqlConnection connection;

    public UserService(MySqlConnection connection)
    {
        this.connection = connection;
    }

    public async ValueTask<bool> CheckExistsUser(string username, string referenceId)
    {
        var p = new DynamicParameters();
        p.Add("UserName", username, dbType: DbType.String);
        p.Add("ReferenceId", referenceId, dbType: DbType.String);

        var adminUser = await connection.QueryFirstOrDefaultAsync<AdminUser>("LoginNewAdminUser_Get_xtest", p, commandType: CommandType.StoredProcedure);

        return adminUser != null && adminUser.Status;
    }
}

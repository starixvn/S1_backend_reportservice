using System.Data;
using System.Diagnostics;
using Dapper;
using ReportService.Core.Models.Enums;
using MySql.Data.MySqlClient;
using ReportService.Models;
using Serilog.Context;

namespace ReportService.Service.DAL
{
    public sealed class MySqlClient
    {
        public readonly DataBase DataBase;

        private readonly string _connectionString;

        private readonly IEnumerable<string> _backupConnectionStringList;

        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString)
        {
            DataBase = dataBase;
            _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _connectionString = connectionString;
            _backupConnectionStringList = null;
        }
        
        public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString, IEnumerable<string> backupConnectionStringList)
        {
            DataBase = dataBase;
            _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _connectionString = connectionString;
            _backupConnectionStringList = backupConnectionStringList;
        }

        private string HealthCheck()
        {            
            if (_backupConnectionStringList == null)
            {
                return _connectionString;
            }

            foreach (var backupConn in _backupConnectionStringList)
            {
                try
                {
                    using var connection = new MySqlConnection(backupConn);
                    var dbSlaveStatus = connection.QueryFirstOrDefault<DBSlaveStatus>("Service_HealthCheck_V2", commandType: CommandType.StoredProcedure);

                    // 拿不到Status或延遲太久都換下一個連線
                    if (dbSlaveStatus == null || dbSlaveStatus.Seconds_Behind_Master > 180)
                    {
                        continue;
                    }

                    return backupConn;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{ReplaceDBString(backupConn)} HealthCheck Exception");
                }           
            }

            // 前面連線檢查都有問題，用最後一個連線
            return _backupConnectionStringList.Last();
        }

        public IEnumerable<T> Query<T>(string name, object? parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);
                var command = new CommandDefinition(name, parameters,
                    commandTimeout: commandTimeout,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result.ToList();
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string name, object? parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                var conn = HealthCheck();
                await using var connection = new MySqlConnection(conn);
                result = await connection.QueryAsync<T>(name, parameters, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout);
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }
        
        private static string ReplaceDBString(string conn)
        {
            return conn.Substring(0, 28);
        }
    }
}
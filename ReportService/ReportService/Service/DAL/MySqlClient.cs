using System.Data;
using System.Diagnostics;
using Dapper;
using ReportService.Core.Models.Enums;
using MySql.Data.MySqlClient;
using Serilog.Context;

namespace ReportService.Service.DAL
{
    public sealed class MySqlClient
    {
        public readonly DataBase DataBase;

        private readonly string _connectionString;

        private readonly string? _backupConnectionString;

        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(DataBase dataBase, ILogger<MySqlClient> logger, string connectionString, string? backupConnectionString = null)
        {
            DataBase = dataBase;
            _logger = logger;
            _connectionString = connectionString;
            _backupConnectionString = backupConnectionString;
        }

        private string HealthCheck()
        {
            if (_backupConnectionString == null)
            {
                return _connectionString;
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var check = connection.QueryFirst<bool>("Service_HealthCheck", commandType: CommandType.StoredProcedure);
                return check ? _connectionString : _backupConnectionString;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthCheck Exception");
                return _backupConnectionString;
            }
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
    }
}
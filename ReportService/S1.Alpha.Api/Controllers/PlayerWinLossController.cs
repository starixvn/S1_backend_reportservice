using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;
using System.Data;
using S1.Alpha.Api.ViewModels;

namespace S1.Alpha.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerWinLossController : ControllerBase
    {
        [HttpGet]
        public async Task<IEnumerable<PlayerWinLossModel>> GetWinLossReportByReferenceAsync(
            [FromServices] MySqlConnection connection,
            [FromQuery] GetPlayerWinLossByRefIdRequest request)
        {
            var timeOffset = request.From.Offset;
            var timeZone = $"{(timeOffset < TimeSpan.Zero ? "-" : "+")}{timeOffset:hh\\:mm}";
            var refIds = string.IsNullOrEmpty(request.ReferenceIds) ? string.Empty : request.ReferenceIds;

            var p = new DynamicParameters();
            p.Add("StartTime", request.From, dbType: DbType.DateTime);
            p.Add("EndTime", request.To, dbType: DbType.DateTime);
            p.Add("TimeZone", timeZone, dbType: DbType.String);
            p.Add("ReferenceIds", $"[{refIds}]", dbType: DbType.String);

            var result = await connection.QueryAsync<PlayerWinLossModel>("Relay_TpBetRecord_Get_xtest", p,commandType: CommandType.StoredProcedure);

            foreach (var item in result)
            {
                if (!string.IsNullOrEmpty(item.WinLossDate))
                {
                    item.WinLossDate = DateTime.Parse(item.WinLossDate).ToString($"yyyy-MM-dd HH:mm:ss tt {timeZone}");
                }
            };

            return result;
        }
    }
}

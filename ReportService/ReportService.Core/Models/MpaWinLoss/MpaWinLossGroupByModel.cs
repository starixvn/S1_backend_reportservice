using Newtonsoft.Json;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossGroupByModel : MpaWinLossGroupBySummary, IFixDateTimeOffset
{
    public MpaWinLossGroupByModel()
    {
    }

    public MpaWinLossGroupByModel(MpaWinLossGroupBySummary data): base(data)
    {
    }

    [JsonIgnore]
    public DateTime Date { get; set; }

    [JsonIgnore]
    public int CustId { get; set; }
    
    public string Key { get; set; }
    public string ChildKey { get; set; }
}
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class MpaWinLossGroupBySummaryTpGameType : MpaWinLossGroupBySummary
{
    public MpaWinLossGroupBySummaryTpGameType()
    {
    }
    
    public MpaWinLossGroupBySummaryTpGameType(IMpaWinLossGroupBySummary data, string tpGameTypeName)
        : base(data)
    {
        TpGameTypeName = tpGameTypeName;
    }
    
    public string TpGameTypeName { get; set; }
}

public class TpBetRecordByTpGameTypeWrapper
{
    public IEnumerable<MpaWinLossGroupBySummaryTpGameType> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
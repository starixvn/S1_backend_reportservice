using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class TpBetRecordGroupByTpGameType : TpBetRecordGroupBy
{
    public TpBetRecordGroupByTpGameType()
    {
    }
    
    public TpBetRecordGroupByTpGameType(ITpBetRecordGroupBy data, string tpGameTypeName)
        : base(data)
    {
        TpGameTypeName = tpGameTypeName;
    }
    
    public string TpGameTypeName { get; set; }
}

public class TpBetRecordByTpGameTypeWrapper
{
    public IEnumerable<TpBetRecordGroupByTpGameType> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
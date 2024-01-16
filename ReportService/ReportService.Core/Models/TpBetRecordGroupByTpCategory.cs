using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class TpBetRecordGroupByTpCategory : TpBetRecordGroupBy
{
    public TpBetRecordGroupByTpCategory()
    {
    }
    
    public TpBetRecordGroupByTpCategory(ITpBetRecordGroupBy data, string tpCategoryName)
        : base(data)
    {
        TpCategoryName = tpCategoryName;
    }
    
    public string TpCategoryName { get; set; }
}

public class TpBetRecordByTpCategoryWrapper
{
    public IEnumerable<TpBetRecordGroupByTpCategory> TpBetRecordResult { get; set; }

    public int UniquePlayers { get; set; }
}
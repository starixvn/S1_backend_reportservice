using ReportService.Core.Models.Enums;

namespace ReportService.Core.Models.MpaWinLoss;

public struct MpaWinLossDefinition
{
    public static readonly IEnumerable<MpaWinLossGroupByTypeEnum> SummaryTransaction = new[]
    {
        MpaWinLossGroupByTypeEnum.Date,
        MpaWinLossGroupByTypeEnum.Month,
        MpaWinLossGroupByTypeEnum.Player,
        MpaWinLossGroupByTypeEnum.Affiliate,
        MpaWinLossGroupByTypeEnum.Brand,
        MpaWinLossGroupByTypeEnum.Currency
    };
    
    public static readonly IEnumerable<MpaWinLossGroupByTypeEnum> UseLongerCache = new[]
    {
        MpaWinLossGroupByTypeEnum.Player,
        MpaWinLossGroupByTypeEnum.Affiliate
    };
    
    public static readonly IEnumerable<MpaWinLossGroupByTypeEnum> HasPlayerInfo = new[]
    {
        MpaWinLossGroupByTypeEnum.Player,
        MpaWinLossGroupByTypeEnum.Affiliate
    };
}
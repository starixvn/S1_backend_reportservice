using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossRequest : IMultiSiteApiRequest
{
    /// <summary>
    /// 結算起始時間
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// 結算結束時間
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// 所有供應商的所有類型遊戲
    /// </summary>
    public IEnumerable<int>? TpGameTypes { get; set; }

    /// <summary>
    /// 玩家名稱
    /// </summary>
    public IEnumerable<string>? UserNames { get; set; }

    /// <summary>
    /// 所有幣別
    /// </summary>
    public IEnumerable<CurrencyEnum>? CurrencyIds { get; set; }

    /// <summary>
    /// 包含免費旋轉 => TicketStatus = FreeBetWin(6)
    /// </summary>
    public bool IncludeFreeSpins { get; set; }

    /// <summary>
    /// 包含免費旋轉 => TicketStatus = draw(4)
    /// </summary>
    public bool NoDraw { get; set; }

    public CurrencyEnum BaseCurrencyId { get; set; }
    
    /// <summary>
    /// SettledDate = 1,
    /// CreatedDate = 2
    /// </summary>
    public MpaDateType DateType { get; set; }

    /// <summary>
    /// 玩家類型，一般、代理、值播主
    /// </summary>
    public IEnumerable<MemberTypeEnum>? MemberTypes { get; set; }

    /// <summary>
    /// 是否包含測試資料
    /// </summary>
    public bool IncludeTest { get; set; }

    /// <summary>
    /// 各個TpCategory
    /// </summary>
    public IEnumerable<int>? TpCategorys { get; set; }
    public IEnumerable<int> SiteIds { get; set; }
    
    /// <summary>
    /// 代理帳號
    /// </summary>
    public IEnumerable<string>? AffiliateNames { get; set; }

    public MpaWinLossGroupByTypeEnum GroupByType { get; set; }
}
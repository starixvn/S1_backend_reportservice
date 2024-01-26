using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class MpaClaimBonusDto : IFixDateTimeOffset, IFilterTestUser
{
    public DateTime Date { get; set; }

    public int CustId { get; set; }

    public string UserName { get; set; }

    public string TpUserName { get; set; }

    public int CurrencyId { get; set; }

    public decimal BonusAmountClaim { get; set; }

    public int IsTest { get; set; }
    public int SiteId { get; set; }
}
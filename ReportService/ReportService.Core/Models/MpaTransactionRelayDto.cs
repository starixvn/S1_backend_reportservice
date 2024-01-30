using ReportService.Core.Models.Enums;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Models;

public class MpaTransactionRelayDto : IFixDateTimeOffset, IFilterTestUser
{
    public DateTime Date { get; set; }
    public int SiteId { get; set; }
    public int CurrencyId { get; set; }
    public int CustId { get; set; }
    public string UserName { get; set; }
    public string TpUserName { get; set; }
    public TransactionRelaySummaryTypeEnum SummaryType { get; set; }
    public byte TransactionType { get; set; }
    public decimal Amount { get; set; }
    public int RowCount { get; set; }
    public int IsTest { get; set; }
    public string AffiliateName { get; set; }
}
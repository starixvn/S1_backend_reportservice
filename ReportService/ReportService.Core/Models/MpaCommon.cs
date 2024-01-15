namespace ReportService.Core.Models;

public class TpGameTypeOptionDto
{
    public int Type { get; set; }

    public string TypeName { get; set; }
}


public class MpaRateDto
{
    public DateTime RateDate { get; set; }

    public int CurrencyId { get; set; }

    public string CurrencyName { get; set; }

    public int TargetCurrencyId { get; set; }

    public string TargetCurrencyName { get; set; }

    public decimal Rate { get; set; }
}

public class TpCategoryDto
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; }
}

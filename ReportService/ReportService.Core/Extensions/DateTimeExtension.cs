namespace ReportService.Core.Extensions;

public static class DateTimeExtension
{
    public static DateTime ToOffSetReturnDate(this DateTime dateTime, TimeSpan offset)
    {
        return new DateTimeOffset(dateTime, new TimeSpan(0, 0, 0)).ToOffset(offset).Date;
    }
    
    public static string DateMapperToRateDate(this DateTime date)
    {
        if (date == DateTime.Today)
            return date.ToString("yyyy-MM-dd");
        
        return date.ToString("yyyy-MM") == DateTime.Now.ToString("yyyy-MM")
            ? DateTime.UtcNow.ToString("yyyy-MM-dd")
            : date.AddMonths(1).AddDays(-date.AddMonths(1).Day).ToString("yyyy-MM-dd");
    }

    public static string DateMapperToRateMonth(this DateTime date)
    {
        if (date == DateTime.Today)
            return date.ToString("yyyy-MM");

        return date.ToString("yyyy-MM") == DateTime.Now.ToString("yyyy-MM")
            ? DateTime.UtcNow.ToString("yyyy-MM")
            : date.AddMonths(1).AddDays(-date.AddMonths(1).Day).ToString("yyyy-MM");
    }
}
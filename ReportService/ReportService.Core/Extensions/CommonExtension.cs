using Newtonsoft.Json;

namespace ReportService.Core.Extensions;

public static class CommonExtension
{
    public static string? SerializeEnumerableEmptyReturnNull<T>(IEnumerable<T> obj)
        => obj.Any() ? JsonConvert.SerializeObject(obj) : null;
}
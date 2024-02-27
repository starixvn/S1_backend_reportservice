using Newtonsoft.Json;
using ReportService.Core.Models.Interface;

namespace ReportService.Core.Extensions;

public static class CommonExtension
{
    public static string SerializeEnumerableEmpty<T>(IEnumerable<T>? obj)
        => obj != null ? JsonConvert.SerializeObject(obj) : "[]";
    
    public static Dictionary<TKey, TValue> SortApiData<TKey, TValue>(Dictionary<TKey, TValue> exportData, string sortField, OrderByDirection sortOrder)
        where TKey : notnull
    {
        if (sortField == "key")
        {
            if (sortOrder == OrderByDirection.Ascending)
            {
                return exportData.OrderBy(kvp => kvp.Key)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            return exportData.OrderByDescending(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        var propertyNames = typeof(TValue)
            .GetProperties()
            .Where(x => string.Equals(x.Name, sortField, StringComparison.CurrentCultureIgnoreCase));
        
        var propertyName = propertyNames.Select(x => x.Name).FirstOrDefault() ?? "";

        var propertyInfo = typeof(TValue).GetProperty(propertyName);
        if (propertyInfo == null)
        {
            throw new ArgumentException("Invalid sort field or type.");
        }

        if (sortOrder == OrderByDirection.Ascending)
        {
            return exportData.OrderBy(kvp => propertyInfo.GetValue(kvp.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return exportData.OrderByDescending(kvp => propertyInfo.GetValue(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
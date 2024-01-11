namespace ReportService.Core.Models.Interface;

public interface IOrderBy
{
    /// <summary>
    /// 是否排序
    /// </summary>
    bool OrderBy { get; set; }
    /// <summary>
    /// 排序欄位名稱
    /// </summary>
    string OrderByProperty { get; set; }
    /// <summary>
    /// 排序類型:
    /// 0=升序
    /// 1=降序
    /// </summary>
    OrderByDirection OrderByDirection { get; set; }
}

public enum OrderByDirection
{
    /// <summary>
    /// 升序
    /// </summary>
    Ascending = 0,
    /// <summary>
    /// 降序
    /// </summary>
    Descending = 1
}
using Newtonsoft.Json;

namespace ReportService.Core.Models;

public class ServerInfo
{
    public int SiteId { set; get; }
    public string SiteName { set; get; }
    public string LangList { set; get; }
    public string CurrencyList { set; get; }
    public string DefLang { set; get; }
    public IEnumerable<int> ICurrencyList =>
        string.IsNullOrWhiteSpace(CurrencyList) ?
            null :
            JsonConvert.DeserializeObject<IEnumerable<int>>(CurrencyList);

    public IEnumerable<string> ILangList
        => JsonConvert.DeserializeObject<IEnumerable<string>>(LangList);
}
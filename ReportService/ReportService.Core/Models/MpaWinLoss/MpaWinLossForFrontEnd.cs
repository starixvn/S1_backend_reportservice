using Newtonsoft.Json;

namespace ReportService.Core.Models.MpaWinLoss;

public class MpaWinLossCurrencyDetailForFrontEnd<T>
{
    public string DisplayKey { get; set; }

    public T Data { get; set; }
}

public class MpaWinLossByDateResponseToFrontEnd
{
    [JsonProperty(Order = -1)]
    public string DisplayKey { get; set; }

    [JsonProperty(Order = 0)]
    public decimal TurnOver { get; set; }

    [JsonProperty(Order = 1)]
    public decimal ValidTurnOver { get; set; }

    [JsonProperty(Order = 2)]
    public decimal GGR { get; set; }

    [JsonProperty(Order = 3)]
    public decimal PlayerWinLoss { get; set; }

    [JsonProperty(Order = 4)]
    public decimal BAPlayerWinLoss { get; set; }

    [JsonProperty(Order = 5)]
    public decimal ProgressiveWin { get; set; }

    [JsonProperty(Order = 6)]
    public decimal ProgressiveBet { get; set; }

    [JsonProperty(Order = 7)]
    public decimal Commission { get; set; }

    [JsonProperty(Order = 8)]
    public decimal BACommission { get; set; }

    [JsonProperty(Order = 9)]
    public int BetCount { get; set; }

    [JsonProperty(Order = 10)]
    public decimal AvgBetAmount { get; set; }

    [JsonProperty(Order = 11)]
    public int UniquePlayers { get; set; }

    [JsonProperty(Order = 12)]
    public decimal Margin { get; set; }

    [JsonProperty(Order = 13)]
    public int DepositCount { get; set; }

    [JsonProperty(Order = 14)]
    public decimal TotalDepositAmount { get; set; }

    [JsonProperty(Order = 15)]
    public int WithDrawCount { get; set; }

    [JsonProperty(Order = 16)]
    public decimal TotalWithDrawAmount { get; set; }

    [JsonProperty(Order = 17)]
    public decimal TotalBonus { get; set; }

    public List<MpaWinLossCurrencyDetailForFrontEnd<MpaWinLossCurrencyDetail>> Children { get; set; }
}
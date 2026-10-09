namespace MarketAnalysis.Models;

public class MarketSummary
{
    public string IndexName { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal ChangePercent { get; set; }
    public decimal ChangeAmount { get; set; }
}

public class SectorPerformance
{
    public string Sector { get; set; } = string.Empty;
    public decimal ChangePercent { get; set; }
    public int GainersCount { get; set; }
    public int LosersCount { get; set; }
}

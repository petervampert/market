namespace MarketAnalysis.Models;

public class Holding
{
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public AssetType AssetType { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DailyChangePercent { get; set; }

    public decimal TotalInvested => Quantity * AverageBuyPrice;
    public decimal CurrentValue => Quantity * CurrentPrice;
    public decimal UnrealizedGain => CurrentValue - TotalInvested;
    public decimal UnrealizedGainPercent =>
        TotalInvested == 0 ? 0 : UnrealizedGain / TotalInvested * 100;
    public decimal DailyGain => CurrentValue * (DailyChangePercent / 100);
}

public class Portfolio
{
    public string Name { get; set; } = string.Empty;
    public List<Holding> Holdings { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];

    public decimal TotalInvested => Holdings.Sum(h => h.TotalInvested);
    public decimal TotalCurrentValue => Holdings.Sum(h => h.CurrentValue);
    public decimal TotalUnrealizedGain => TotalCurrentValue - TotalInvested;
    public decimal TotalUnrealizedGainPercent =>
        TotalInvested == 0 ? 0 : TotalUnrealizedGain / TotalInvested * 100;
    public decimal TotalDailyGain => Holdings.Sum(h => h.DailyGain);
    public decimal TotalDailyGainPercent =>
        TotalCurrentValue == 0 ? 0 : TotalDailyGain / TotalCurrentValue * 100;
}

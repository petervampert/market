namespace MarketAnalysis.Models;

public class Asset
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClosePrice { get; set; }
    public decimal MarketCap { get; set; }
    public decimal Volume { get; set; }
    public decimal Week52High { get; set; }
    public decimal Week52Low { get; set; }

    public decimal DailyChangePercent =>
        PreviousClosePrice == 0 ? 0
        : (CurrentPrice - PreviousClosePrice) / PreviousClosePrice * 100;

    public decimal DailyChangeAmount => CurrentPrice - PreviousClosePrice;
}

public enum AssetType
{
    Stock,
    ETF,
    Crypto,
    Bond
}

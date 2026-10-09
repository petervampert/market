using MarketAnalysis.Models;

namespace MarketAnalysis.Data;

/// <summary>
/// Provides realistic-looking sample market data and a demo portfolio.
/// In a real app this would be replaced by a live market data API (e.g. Alpha Vantage, Yahoo Finance).
/// </summary>
public static class MarketDataSeeder
{
    private static readonly Random Rng = new(42);

    public static List<Asset> GetAssets() =>
    [
        // Stocks
        new Asset { Symbol = "AAPL",  Name = "Apple Inc.",            Type = AssetType.Stock,  CurrentPrice = 213.18m, PreviousClosePrice = 210.62m, MarketCap = 3_280_000_000_000m, Volume = 58_400_000,  Week52High = 237.23m, Week52Low = 164.08m },
        new Asset { Symbol = "MSFT",  Name = "Microsoft Corp.",        Type = AssetType.Stock,  CurrentPrice = 415.32m, PreviousClosePrice = 418.74m, MarketCap = 3_090_000_000_000m, Volume = 22_100_000,  Week52High = 468.35m, Week52Low = 385.58m },
        new Asset { Symbol = "GOOGL", Name = "Alphabet Inc.",          Type = AssetType.Stock,  CurrentPrice = 174.40m, PreviousClosePrice = 171.93m, MarketCap = 2_170_000_000_000m, Volume = 24_800_000,  Week52High = 193.31m, Week52Low = 130.67m },
        new Asset { Symbol = "AMZN",  Name = "Amazon.com Inc.",        Type = AssetType.Stock,  CurrentPrice = 198.90m, PreviousClosePrice = 202.15m, MarketCap = 2_100_000_000_000m, Volume = 38_500_000,  Week52High = 215.90m, Week52Low = 151.61m },
        new Asset { Symbol = "NVDA",  Name = "NVIDIA Corp.",           Type = AssetType.Stock,  CurrentPrice = 875.40m, PreviousClosePrice = 859.05m, MarketCap = 2_160_000_000_000m, Volume = 44_200_000,  Week52High = 974.00m, Week52Low = 402.97m },
        new Asset { Symbol = "TSLA",  Name = "Tesla Inc.",             Type = AssetType.Stock,  CurrentPrice = 185.10m, PreviousClosePrice = 192.30m, MarketCap =   590_000_000_000m, Volume = 92_300_000,  Week52High = 278.98m, Week52Low = 138.80m },
        new Asset { Symbol = "META",  Name = "Meta Platforms Inc.",    Type = AssetType.Stock,  CurrentPrice = 527.80m, PreviousClosePrice = 521.40m, MarketCap =   1_340_000_000_000m, Volume = 17_600_000, Week52High = 589.91m, Week52Low = 394.78m },
        new Asset { Symbol = "JPM",   Name = "JPMorgan Chase & Co.",   Type = AssetType.Stock,  CurrentPrice = 214.50m, PreviousClosePrice = 211.80m, MarketCap =   616_000_000_000m, Volume = 11_200_000,  Week52High = 236.48m, Week52Low = 183.58m },
        new Asset { Symbol = "JNJ",   Name = "Johnson & Johnson",      Type = AssetType.Stock,  CurrentPrice = 145.20m, PreviousClosePrice = 146.90m, MarketCap =   349_000_000_000m, Volume =  7_400_000,  Week52High = 168.65m, Week52Low = 141.32m },
        new Asset { Symbol = "V",     Name = "Visa Inc.",              Type = AssetType.Stock,  CurrentPrice = 279.50m, PreviousClosePrice = 276.30m, MarketCap =   571_000_000_000m, Volume =  6_800_000,  Week52High = 290.96m, Week52Low = 233.00m },
        // ETFs
        new Asset { Symbol = "SPY",   Name = "SPDR S&P 500 ETF",       Type = AssetType.ETF,    CurrentPrice = 541.20m, PreviousClosePrice = 538.45m, MarketCap = 0, Volume = 78_000_000,  Week52High = 565.16m, Week52Low = 491.27m },
        new Asset { Symbol = "QQQ",   Name = "Invesco QQQ Trust",      Type = AssetType.ETF,    CurrentPrice = 469.30m, PreviousClosePrice = 466.10m, MarketCap = 0, Volume = 43_200_000,  Week52High = 503.52m, Week52Low = 399.84m },
        new Asset { Symbol = "VTI",   Name = "Vanguard Total Stock Mkt",Type = AssetType.ETF,   CurrentPrice = 254.70m, PreviousClosePrice = 253.20m, MarketCap = 0, Volume = 4_100_000,   Week52High = 265.29m, Week52Low = 221.30m },
        // Crypto
        new Asset { Symbol = "BTC",   Name = "Bitcoin",                Type = AssetType.Crypto, CurrentPrice = 63_420m, PreviousClosePrice = 61_850m, MarketCap = 1_250_000_000_000m, Volume = 38_000_000_000m, Week52High = 73_835m, Week52Low = 26_011m },
        new Asset { Symbol = "ETH",   Name = "Ethereum",               Type = AssetType.Crypto, CurrentPrice = 3_285m,  PreviousClosePrice = 3_340m,  MarketCap =   394_000_000_000m, Volume =  16_000_000_000m, Week52High = 4_093m,  Week52Low = 1_518m  },
        // Bonds
        new Asset { Symbol = "TLT",   Name = "iShares 20+ Year T-Bond", Type = AssetType.Bond,  CurrentPrice =  96.40m, PreviousClosePrice =  95.80m, MarketCap = 0, Volume = 22_400_000,  Week52High = 101.58m, Week52Low =  85.39m },
    ];

    public static List<MarketSummary> GetMarketIndices() =>
    [
        new MarketSummary { IndexName = "S&P 500",    Value = 5_412.30m, ChangeAmount =  32.15m, ChangePercent =  0.60m },
        new MarketSummary { IndexName = "NASDAQ",     Value = 17_150.40m,ChangeAmount =  98.70m, ChangePercent =  0.58m },
        new MarketSummary { IndexName = "DOW JONES",  Value = 39_280.20m,ChangeAmount = -85.40m, ChangePercent = -0.22m },
        new MarketSummary { IndexName = "RUSSELL 2000",Value = 2_095.60m,ChangeAmount =  14.30m, ChangePercent =  0.69m },
        new MarketSummary { IndexName = "VIX",        Value =    17.45m, ChangeAmount =  -0.83m, ChangePercent = -4.54m },
    ];

    public static List<SectorPerformance> GetSectorPerformance() =>
    [
        new SectorPerformance { Sector = "Technology",       ChangePercent =  1.24m, GainersCount = 18, LosersCount =  5 },
        new SectorPerformance { Sector = "Healthcare",       ChangePercent = -0.38m, GainersCount = 10, LosersCount = 13 },
        new SectorPerformance { Sector = "Financials",       ChangePercent =  0.72m, GainersCount = 14, LosersCount =  8 },
        new SectorPerformance { Sector = "Consumer Discret.",ChangePercent = -0.91m, GainersCount =  7, LosersCount = 16 },
        new SectorPerformance { Sector = "Energy",           ChangePercent =  0.45m, GainersCount = 11, LosersCount =  9 },
        new SectorPerformance { Sector = "Utilities",        ChangePercent =  0.18m, GainersCount = 10, LosersCount =  8 },
        new SectorPerformance { Sector = "Real Estate",      ChangePercent = -0.55m, GainersCount =  6, LosersCount = 14 },
        new SectorPerformance { Sector = "Industrials",      ChangePercent =  0.33m, GainersCount = 12, LosersCount =  9 },
    ];

    public static (List<Transaction> Transactions, string PortfolioName) GetDemoTransactions()
    {
        var transactions = new List<Transaction>
        {
            // AAPL — bought 3 years ago, added later
            new() { AssetSymbol = "AAPL", Type = TransactionType.Buy, Quantity = 20,  PricePerUnit = 142.50m, Date = DateTime.Now.AddYears(-3),            Fees = 1.00m },
            new() { AssetSymbol = "AAPL", Type = TransactionType.Buy, Quantity = 10,  PricePerUnit = 170.30m, Date = DateTime.Now.AddYears(-2),            Fees = 1.00m },

            // MSFT
            new() { AssetSymbol = "MSFT", Type = TransactionType.Buy, Quantity = 15,  PricePerUnit = 290.00m, Date = DateTime.Now.AddYears(-3).AddMonths(-6), Fees = 1.50m },

            // NVDA — big winner
            new() { AssetSymbol = "NVDA", Type = TransactionType.Buy, Quantity = 10,  PricePerUnit = 410.00m, Date = DateTime.Now.AddYears(-1).AddMonths(-8), Fees = 1.00m },

            // TSLA — partial sell
            new() { AssetSymbol = "TSLA", Type = TransactionType.Buy, Quantity = 30,  PricePerUnit = 215.00m, Date = DateTime.Now.AddYears(-2).AddMonths(-3), Fees = 1.50m },
            new() { AssetSymbol = "TSLA", Type = TransactionType.Sell,Quantity = 10,  PricePerUnit = 260.00m, Date = DateTime.Now.AddYears(-1),              Fees = 1.50m },

            // SPY ETF
            new() { AssetSymbol = "SPY",  Type = TransactionType.Buy, Quantity = 5,   PricePerUnit = 450.00m, Date = DateTime.Now.AddYears(-2),            Fees = 0.00m },

            // BTC — crypto
            new() { AssetSymbol = "BTC",  Type = TransactionType.Buy, Quantity = 0.5m,PricePerUnit = 42_000m, Date = DateTime.Now.AddYears(-1).AddMonths(-3), Fees = 25.00m },

            // TLT — bond
            new() { AssetSymbol = "TLT",  Type = TransactionType.Buy, Quantity = 50,  PricePerUnit = 92.00m,  Date = DateTime.Now.AddMonths(-8),           Fees = 0.00m },

            // META
            new() { AssetSymbol = "META", Type = TransactionType.Buy, Quantity = 8,   PricePerUnit = 480.00m, Date = DateTime.Now.AddYears(-1),            Fees = 1.00m },
        };

        return (transactions, "My Demo Portfolio");
    }
}

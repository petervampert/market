using MarketAnalysis.Models;

namespace MarketAnalysis.Services.LiveData;

/// <summary>
/// Contract for fetching live market data.
/// Swap the implementation (Finnhub, Alpha Vantage, etc.) without touching the rest of the app.
/// </summary>
public interface IMarketDataFetcher
{
    /// <summary>Fetch current quotes for the given symbols.</summary>
    Task<List<AssetQuote>> FetchQuotesAsync(IEnumerable<string> symbols, CancellationToken ct = default);

    /// <summary>Fetch major index values.</summary>
    Task<List<IndexQuote>> FetchIndicesAsync(CancellationToken ct = default);
}

public record AssetQuote(
    string  Symbol,
    decimal CurrentPrice,
    decimal PreviousClose,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Volume,
    decimal MarketCap,
    decimal Week52High,
    decimal Week52Low
);

public record IndexQuote(
    string  Name,           // human label, e.g. "S&P 500"
    string  Symbol,         // ticker used to fetch, e.g. "^GSPC"
    decimal Value,
    decimal ChangeAmount,
    decimal ChangePercent
);

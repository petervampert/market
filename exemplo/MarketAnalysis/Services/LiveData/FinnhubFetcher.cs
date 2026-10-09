using System.Text.Json;
using System.Text.Json.Nodes;
using MarketAnalysis.Models;

namespace MarketAnalysis.Services.LiveData;

/// <summary>
/// Fetches live quotes from the Finnhub REST API (https://finnhub.io).
/// Free tier: 60 requests/minute — plenty for 16 symbols every 10 minutes.
///
/// Set your API key in appsettings.json:
///   "Finnhub": { "ApiKey": "YOUR_KEY_HERE" }
///
/// Get a free key at https://finnhub.io/register
/// </summary>
public class FinnhubFetcher : IMarketDataFetcher
{
    private readonly HttpClient _http;
    private readonly string     _apiKey;
    private readonly ILogger<FinnhubFetcher> _log;

    // Map human-readable index names to their Finnhub/Yahoo-style symbols.
    // Finnhub uses the same "^" prefix for indices as Yahoo Finance.
    private static readonly (string Name, string Symbol)[] IndexMap =
    [
        ("S&P 500",     "^GSPC"),
        ("NASDAQ",      "^IXIC"),
        ("DOW JONES",   "^DJI"),
        ("RUSSELL 2000","^RUT"),
        ("VIX",         "^VIX"),
    ];

    public FinnhubFetcher(IHttpClientFactory factory, IConfiguration config, ILogger<FinnhubFetcher> log)
    {
        _http   = factory.CreateClient("finnhub");
        _apiKey = config["Finnhub:ApiKey"] ?? string.Empty;
        _log    = log;
    }

    // ── Quotes ───────────────────────────────────────────────────────────────

    public async Task<List<AssetQuote>> FetchQuotesAsync(
        IEnumerable<string> symbols, CancellationToken ct = default)
    {
        var results = new List<AssetQuote>();

        // Finnhub free tier: one symbol per request, but 60 req/min is fine.
        // We fire them concurrently (max 10 at a time to stay polite).
        var semaphore = new SemaphoreSlim(10);
        var tasks = symbols.Select(async sym =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var quote   = await GetQuoteAsync(sym, ct);
                var profile = await GetProfileAsync(sym, ct);
                if (quote is null) return null;

                return new AssetQuote(
                    Symbol:        sym,
                    CurrentPrice:  quote.Value.GetProperty("c").GetDecimal(),
                    PreviousClose: quote.Value.GetProperty("pc").GetDecimal(),
                    Open:          quote.Value.GetProperty("o").GetDecimal(),
                    High:          quote.Value.GetProperty("h").GetDecimal(),
                    Low:           quote.Value.GetProperty("l").GetDecimal(),
                    Volume:        GetLong(quote.Value, "v"),
                    MarketCap:     profile.HasValue ? GetDecimalOrDefault(profile.Value, "marketCapitalization") * 1_000_000m : 0,
                    Week52High:    profile.HasValue ? GetDecimalOrDefault(profile.Value, "52WeekHigh") : 0,
                    Week52Low:     profile.HasValue ? GetDecimalOrDefault(profile.Value, "52WeekLow")  : 0
                );
            }
            catch (Exception ex)
            {
                _log.LogWarning("Quote fetch failed for {Symbol}: {Msg}", sym, ex.Message);
                return null;
            }
            finally { semaphore.Release(); }
        });

        foreach (var result in await Task.WhenAll(tasks))
            if (result is not null) results.Add(result);

        return results;
    }

    // ── Indices ──────────────────────────────────────────────────────────────

    public async Task<List<IndexQuote>> FetchIndicesAsync(CancellationToken ct = default)
    {
        var results = new List<IndexQuote>();

        foreach (var (name, symbol) in IndexMap)
        {
            try
            {
                var quote = await GetQuoteAsync(symbol, ct);
                if (quote is null) continue;

                var cur    = quote.Value.GetProperty("c").GetDecimal();
                var prev   = quote.Value.GetProperty("pc").GetDecimal();
                var change = cur - prev;
                var pct    = prev == 0 ? 0 : change / prev * 100;

                results.Add(new IndexQuote(name, symbol, cur, change, pct));
            }
            catch (Exception ex)
            {
                _log.LogWarning("Index fetch failed for {Symbol}: {Msg}", symbol, ex.Message);
            }
        }

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<JsonElement?> GetQuoteAsync(string symbol, CancellationToken ct)
    {
        var url = $"https://finnhub.io/api/v1/quote?symbol={Uri.EscapeDataString(symbol)}&token={_apiKey}";
        using var resp = await _http.GetAsync(url, ct);

        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("Finnhub quote HTTP {Code} for {Symbol}", resp.StatusCode, symbol);
            return null;
        }

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        // Finnhub returns {"c":0,"d":null,...} when the symbol is unknown
        if (!root.TryGetProperty("c", out var price) || price.GetDecimal() == 0)
            return null;

        // Keep the element alive after the document scope ends
        return root.Clone();
    }

    private async Task<JsonElement?> GetProfileAsync(string symbol, CancellationToken ct)
    {
        // Skip indices — they have no company profile
        if (symbol.StartsWith('^')) return null;

        try
        {
            var url = $"https://finnhub.io/api/v1/stock/profile2?symbol={Uri.EscapeDataString(symbol)}&token={_apiKey}";
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return null;

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var doc  = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return doc.RootElement.Clone();
        }
        catch { return null; }
    }

    private static decimal GetDecimalOrDefault(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number)
            return v.GetDecimal();
        return 0;
    }

    private static decimal GetLong(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number)
            return v.GetDecimal();
        return 0;
    }
}

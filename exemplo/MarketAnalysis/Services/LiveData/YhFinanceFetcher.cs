using System.Text.Json;
using MarketAnalysis.Models;

namespace MarketAnalysis.Services.LiveData;

/// <summary>
/// Fetches live quotes via the "YH Finance" proxy on RapidAPI
/// (https://rapidapi.com/sparior/api/yahoo-finance15).
///
/// Free tier: ~500 requests/month (plenty for 2-minute refreshes of 16 symbols
/// in a single batch call — that's 1 request per refresh, ~720 req/day if you
/// run it continuously, so choose an interval that fits your plan).
///
/// Set your key in appsettings.json:
///   "RapidApi": { "ApiKey": "YOUR_KEY", "UpdateIntervalSeconds": 120 }
///
/// Get a free key at https://rapidapi.com/sparior/api/yahoo-finance15
/// </summary>
public class YhFinanceFetcher : IMarketDataFetcher
{
    // All asset tickers + index tickers are fetched in ONE batch call.
    // Index symbols must use the Yahoo Finance "^" prefix.
    private static readonly Dictionary<string, string> IndexSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["S&P 500"]     = "^GSPC",
        ["NASDAQ"]      = "^IXIC",
        ["DOW JONES"]   = "^DJI",
        ["RUSSELL 2000"]= "^RUT",
        ["VIX"]         = "^VIX",
    };

    private readonly HttpClient _http;
    private readonly string     _apiKey;
    private readonly ILogger<YhFinanceFetcher> _log;

    private const string BaseUrl = "https://yahoo-finance15.p.rapidapi.com/api/v1/markets/quote";

    public YhFinanceFetcher(
        IHttpClientFactory factory,
        IConfiguration config,
        ILogger<YhFinanceFetcher> log)
    {
        _http   = factory.CreateClient("yhfinance");
        _apiKey = config["RapidApi:ApiKey"] ?? string.Empty;
        _log    = log;
    }

    // ── Quotes ───────────────────────────────────────────────────────────────

    public async Task<List<AssetQuote>> FetchQuotesAsync(
        IEnumerable<string> symbols, CancellationToken ct = default)
    {
        // Batch all asset symbols in a single request — much cheaper on quota.
        var tickers = string.Join(",", symbols);
        var root    = await CallApiAsync(tickers, ct);
        if (root is null) return [];

        var results = new List<AssetQuote>();

        // Response shape:
        // { "body": { "AAPL": { "regularMarketPrice": …, … }, "MSFT": { … } } }
        // or body is an array — handle both.
        if (!root.Value.TryGetProperty("body", out var body)) return results;

        if (body.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in body.EnumerateObject())
            {
                // Skip index symbols that leaked into the asset call
                if (prop.Name.StartsWith('^')) continue;
                var q = ParseQuote(prop.Name, prop.Value);
                if (q is not null) results.Add(q);
            }
        }
        else if (body.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in body.EnumerateArray())
            {
                var sym = GetStr(item, "symbol");
                if (string.IsNullOrEmpty(sym) || sym.StartsWith('^')) continue;
                var q = ParseQuote(sym, item);
                if (q is not null) results.Add(q);
            }
        }

        _log.LogDebug("[YhFinance] Parsed {Count} asset quotes.", results.Count);
        return results;
    }

    // ── Indices ──────────────────────────────────────────────────────────────

    public async Task<List<IndexQuote>> FetchIndicesAsync(CancellationToken ct = default)
    {
        // Fetch all index tickers in a single batch call.
        var tickers = string.Join(",", IndexSymbols.Values);
        var root    = await CallApiAsync(tickers, ct);
        if (root is null) return [];

        var results = new List<IndexQuote>();

        if (!root.Value.TryGetProperty("body", out var body)) return results;

        // Build reverse map: "^GSPC" -> "S&P 500"
        var reverseMap = IndexSymbols.ToDictionary(kv => kv.Value, kv => kv.Key,
            StringComparer.OrdinalIgnoreCase);

        void ParseIndexItem(string sym, JsonElement el)
        {
            if (!reverseMap.TryGetValue(sym, out var name)) return;
            var cur    = GetDecimal(el, "regularMarketPrice");
            var prev   = GetDecimal(el, "regularMarketPreviousClose");
            var change = GetDecimal(el, "regularMarketChange");
            var pct    = GetDecimal(el, "regularMarketChangePercent");

            // Fallback: compute from price - prev if fields not present
            if (change == 0 && cur != 0 && prev != 0) change = cur - prev;
            if (pct   == 0 && prev != 0)              pct    = change / prev * 100;

            if (cur > 0)
                results.Add(new IndexQuote(name, sym, cur, change, pct));
        }

        if (body.ValueKind == JsonValueKind.Object)
            foreach (var prop in body.EnumerateObject())
                ParseIndexItem(prop.Name, prop.Value);
        else if (body.ValueKind == JsonValueKind.Array)
            foreach (var item in body.EnumerateArray())
                ParseIndexItem(GetStr(item, "symbol"), item);

        _log.LogDebug("[YhFinance] Parsed {Count} index quotes.", results.Count);
        return results;
    }

    // ── Core HTTP call ───────────────────────────────────────────────────────

    private async Task<JsonElement?> CallApiAsync(string tickersCsv, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _log.LogWarning("[YhFinance] No RapidAPI key configured. Set RapidApi:ApiKey in appsettings.json.");
            return null;
        }

        var url = $"{BaseUrl}?ticker={Uri.EscapeDataString(tickersCsv)}&type=STOCKS";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-RapidAPI-Key",  _apiKey);
        req.Headers.Add("X-RapidAPI-Host", "yahoo-finance15.p.rapidapi.com");

        try
        {
            using var resp = await _http.SendAsync(req, ct);

            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("[YhFinance] HTTP {Code} from RapidAPI. Check your API key and quota.", resp.StatusCode);
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[YhFinance] Request failed for tickers: {Tickers}", tickersCsv);
            return null;
        }
    }

    // ── Parsers ──────────────────────────────────────────────────────────────

    private static AssetQuote? ParseQuote(string symbol, JsonElement el)
    {
        var price = GetDecimal(el, "regularMarketPrice");
        if (price <= 0) return null;

        var prev   = GetDecimal(el, "regularMarketPreviousClose");
        var open   = GetDecimal(el, "regularMarketOpen");
        var high   = GetDecimal(el, "regularMarketDayHigh");
        var low    = GetDecimal(el, "regularMarketDayLow");
        var volume = GetDecimal(el, "regularMarketVolume");
        var mcap   = GetDecimal(el, "marketCap");
        var w52h   = GetDecimal(el, "fiftyTwoWeekHigh");
        var w52l   = GetDecimal(el, "fiftyTwoWeekLow");

        return new AssetQuote(symbol, price, prev, open, high, low, volume, mcap, w52h, w52l);
    }

    private static decimal GetDecimal(JsonElement el, string key)
    {
        if (!el.TryGetProperty(key, out var v)) return 0;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetDecimal(),
            JsonValueKind.String => decimal.TryParse(v.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0,
            _ => 0,
        };
    }

    private static string GetStr(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? string.Empty
            : string.Empty;
}

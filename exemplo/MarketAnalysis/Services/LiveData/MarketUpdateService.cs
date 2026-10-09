using MarketAnalysis.Data;
using MarketAnalysis.Models;

namespace MarketAnalysis.Services.LiveData;

/// <summary>
/// Hosted background service that refreshes the MarketDataStore from Yahoo Finance
/// (via RapidAPI) on a configurable interval.
///
/// Also exposes <see cref="TriggerNowAsync"/> so the API controller can kick off
/// an immediate refresh when the user clicks "Update from Yahoo Finance".
///
/// Config key:  RapidApi:UpdateIntervalSeconds  (default 120 s, min 30 s)
/// </summary>
public class MarketUpdateService : BackgroundService
{
    private readonly IMarketDataFetcher           _fetcher;
    private readonly MarketDataStore              _store;
    private readonly ILogger<MarketUpdateService> _log;
    private readonly TimeSpan                     _interval;

    // Signals the delay loop to wake up and fetch immediately.
    private readonly SemaphoreSlim _trigger = new(0, 1);

    // Prevent two concurrent fetches (e.g. scheduled + manual at the same time).
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    public MarketUpdateService(
        IMarketDataFetcher fetcher,
        MarketDataStore    store,
        IConfiguration     config,
        ILogger<MarketUpdateService> log)
    {
        _fetcher  = fetcher;
        _store    = store;
        _log      = log;
        var secs  = config.GetValue<int>("RapidApi:UpdateIntervalSeconds", 120);
        _interval = TimeSpan.FromSeconds(Math.Max(secs, 30));
    }

    /// <summary>
    /// Called by the refresh API endpoint.
    /// Wakes the background loop immediately if it is sleeping between intervals.
    /// Returns after the fetch completes so the HTTP response reflects fresh data.
    /// </summary>
    public async Task<string> TriggerNowAsync(CancellationToken ct = default)
    {
        // Signal the loop to wake up (non-blocking; drop the signal if already pending)
        _trigger.Release(Math.Max(0, 1 - _trigger.CurrentCount));

        // Wait until the fetch lock is free, meaning the triggered fetch finished.
        await _fetchLock.WaitAsync(ct);
        try
        {
            // Lock is acquired → fetch that was triggered has completed.
            // Run another fetch right now (the loop's fetch may have beaten us here,
            // in which case data is already fresh — that's fine).
            await RefreshAsync(ct);
            return _store.LastError ?? "ok";
        }
        finally { _fetchLock.Release(); }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation(
            "MarketUpdateService started — interval {Secs}s ({Min:F1} min).",
            _interval.TotalSeconds, _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunFetchCycle(stoppingToken);

            // Schedule next automatic run and sleep until then (or until triggered).
            var next = DateTime.UtcNow.Add(_interval);
            _store.SetNextScheduledUpdate(next);

            try
            {
                // WaitAsync returns immediately if TriggerNow released the semaphore,
                // otherwise waits until the interval expires.
                await _trigger.WaitAsync(_interval, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
        }

        _log.LogInformation("MarketUpdateService stopped.");
    }

    // Acquires the fetch lock then runs RefreshAsync.
    private async Task RunFetchCycle(CancellationToken ct)
    {
        await _fetchLock.WaitAsync(ct);
        try   { await RefreshAsync(ct); }
        finally { _fetchLock.Release(); }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        _log.LogInformation("[MarketUpdate] Fetching from Yahoo Finance (RapidAPI)…");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var symbols     = _store.Assets.Select(a => a.Symbol).ToList();
            var quotesTask  = _fetcher.FetchQuotesAsync(symbols, ct);
            var indicesTask = _fetcher.FetchIndicesAsync(ct);
            await Task.WhenAll(quotesTask, indicesTask);

            var quotes  = quotesTask.Result;
            var indices = indicesTask.Result;

            if (quotes.Count == 0)
            {
                var msg = "Live fetch returned no quotes — keeping previous data.";
                _log.LogWarning("[MarketUpdate] {Msg}", msg);
                _store.RecordError(msg);
                return;
            }

            var quoteMap = quotes.ToDictionary(q => q.Symbol, StringComparer.OrdinalIgnoreCase);
            var updatedAssets = _store.Assets.Select(asset =>
            {
                if (!quoteMap.TryGetValue(asset.Symbol, out var q)) return asset;
                return new Asset
                {
                    Symbol             = asset.Symbol,
                    Name               = asset.Name,
                    Type               = asset.Type,
                    CurrentPrice       = q.CurrentPrice   > 0 ? q.CurrentPrice   : asset.CurrentPrice,
                    PreviousClosePrice = q.PreviousClose  > 0 ? q.PreviousClose  : asset.PreviousClosePrice,
                    MarketCap          = q.MarketCap      > 0 ? q.MarketCap      : asset.MarketCap,
                    Volume             = q.Volume         > 0 ? q.Volume         : asset.Volume,
                    Week52High         = q.Week52High     > 0 ? q.Week52High     : asset.Week52High,
                    Week52Low          = q.Week52Low      > 0 ? q.Week52Low      : asset.Week52Low,
                };
            }).ToList();

            var indexMap = indices.ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
            var updatedIndices = _store.Indices.Select(idx =>
            {
                if (!indexMap.TryGetValue(idx.IndexName, out var live)) return idx;
                return new MarketSummary
                {
                    IndexName     = idx.IndexName,
                    Value         = live.Value         != 0 ? live.Value         : idx.Value,
                    ChangeAmount  = live.ChangeAmount  != 0 ? live.ChangeAmount  : idx.ChangeAmount,
                    ChangePercent = live.ChangePercent != 0 ? live.ChangePercent : idx.ChangePercent,
                };
            }).ToList();

            _store.Refresh(updatedAssets, updatedIndices, _store.Sectors);

            sw.Stop();
            _log.LogInformation(
                "[MarketUpdate] Done — {Q} quotes, {I} indices in {Ms}ms. Next in {Secs}s.",
                quotes.Count, indices.Count, sw.ElapsedMilliseconds, (int)_interval.TotalSeconds);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _store.RecordError($"Fetch error: {ex.Message}");
            _log.LogError(ex, "[MarketUpdate] Unhandled error during refresh.");
        }
    }
}

using MarketAnalysis.Models;
using MarketAnalysis.Services;

namespace MarketAnalysis.Data;

/// <summary>
/// Thread-safe singleton that holds all in-memory market data.
/// The background service calls Refresh() every 10 minutes with live data.
/// Controllers always read the latest snapshot atomically.
/// </summary>
public class MarketDataStore
{
    private readonly AnalysisService _svc;
    private readonly ReaderWriterLockSlim _lock = new();

    // ── Snapshot fields (swapped atomically under write-lock) ────────────────
    private List<Asset>             _assets;
    private List<MarketSummary>     _indices;
    private List<SectorPerformance> _sectors;
    private Models.Portfolio        _portfolio;
    private List<RoiResult>         _roiResults;
    private AllocationReport        _allocation;
    private List<Asset>             _topGainers;
    private List<Asset>             _topLosers;
    private DateTime                _lastUpdated;
    private string                  _dataSource = "seed";   // "seed" | "live"
    private string?                 _lastError;

    // ── Public read accessors (snapshot under read-lock) ────────────────────
    public List<Asset>             Assets      => ReadLocked(() => _assets);
    public List<MarketSummary>     Indices     => ReadLocked(() => _indices);
    public List<SectorPerformance> Sectors     => ReadLocked(() => _sectors);
    public List<Transaction>       Transactions { get; }   // never changes
    public Models.Portfolio        Portfolio   => ReadLocked(() => _portfolio);
    public List<RoiResult>         RoiResults  => ReadLocked(() => _roiResults);
    public AllocationReport        Allocation  => ReadLocked(() => _allocation);
    public List<Asset>             TopGainers  => ReadLocked(() => _topGainers);
    public List<Asset>             TopLosers   => ReadLocked(() => _topLosers);
    public DateTime                LastUpdated         => ReadLocked(() => _lastUpdated);
    public string                  DataSource          => ReadLocked(() => _dataSource);
    public string?                 LastError           => ReadLocked(() => _lastError);
    public DateTime                NextScheduledUpdate => ReadLocked(() => _nextScheduledUpdate);

    private DateTime _nextScheduledUpdate = DateTime.UtcNow;

    public MarketDataStore(AnalysisService svc)
    {
        _svc = svc;

        // Seed with static data immediately so the app is usable before the
        // first live fetch completes.
        _assets  = MarketDataSeeder.GetAssets();
        _indices = MarketDataSeeder.GetMarketIndices();
        _sectors = MarketDataSeeder.GetSectorPerformance();

        var (txs, name) = MarketDataSeeder.GetDemoTransactions();
        Transactions = txs;

        (_portfolio, _roiResults, _allocation, _topGainers, _topLosers) = Derive(name);
        _lastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Called by the background service with fresh data from a live source.
    /// Rebuilds all derived objects and swaps everything in under a write-lock.
    /// </summary>
    public void Refresh(
        List<Asset>             newAssets,
        List<MarketSummary>     newIndices,
        List<SectorPerformance> newSectors,
        string?                 error = null)
    {
        var portfolioName = ReadLocked(() => _portfolio.Name);

        // Build derived data outside the lock (CPU work, no contention)
        var assets   = newAssets;
        var indices  = newIndices;
        var sectors  = newSectors;

        // Temporarily replace assets so Derive() uses new prices
        _lock.EnterWriteLock();
        try
        {
            _assets      = assets;
            _indices     = indices;
            _sectors     = sectors;
            _lastError   = error;
            _dataSource  = error == null ? "live" : _dataSource; // keep "live" if partial error
            _lastUpdated = DateTime.UtcNow;
        }
        finally { _lock.ExitWriteLock(); }

        // Rebuild derived (outside write-lock — reads _assets which is now stable)
        var (portfolio, roi, alloc, gainers, losers) = Derive(portfolioName);

        _lock.EnterWriteLock();
        try
        {
            _portfolio  = portfolio;
            _roiResults = roi;
            _allocation = alloc;
            _topGainers = gainers;
            _topLosers  = losers;
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Records a fetch error without changing price data.</summary>
    public void RecordError(string error)
    {
        _lock.EnterWriteLock();
        try { _lastError = error; }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Called by the background service to set when it will next fire.</summary>
    public void SetNextScheduledUpdate(DateTime next)
    {
        _lock.EnterWriteLock();
        try { _nextScheduledUpdate = next; }
        finally { _lock.ExitWriteLock(); }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private (Models.Portfolio, List<RoiResult>, AllocationReport, List<Asset>, List<Asset>)
        Derive(string portfolioName)
    {
        // Read current assets snapshot (no lock needed — called after write)
        var assets    = _assets;
        var portfolio = _svc.BuildPortfolio(portfolioName, Transactions, assets);
        var roi       = _svc.CalculateRoi(portfolio, Transactions);
        var alloc     = _svc.GetAllocation(portfolio);
        var (g, l)    = _svc.GetTopMovers(assets, 5);
        return (portfolio, roi, alloc, g, l);
    }

    private T ReadLocked<T>(Func<T> fn)
    {
        _lock.EnterReadLock();
        try { return fn(); }
        finally { _lock.ExitReadLock(); }
    }
}

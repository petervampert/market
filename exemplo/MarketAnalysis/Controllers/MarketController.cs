using MarketAnalysis.Data;
using MarketAnalysis.Models;
using MarketAnalysis.Services.LiveData;
using Microsoft.AspNetCore.Mvc;

namespace MarketAnalysis.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketController(MarketDataStore store, MarketUpdateService updater) : ControllerBase
{
    // ── Market data reads ────────────────────────────────────────────────────

    [HttpGet("overview")]
    public IActionResult GetOverview() => Ok(new
    {
        lastUpdated = store.LastUpdated,
        indices     = store.Indices,
        sectors     = store.Sectors,
        topGainers  = store.TopGainers.Select(AssetDto),
        topLosers   = store.TopLosers.Select(AssetDto),
    });

    [HttpGet("assets")]
    public IActionResult GetAssets() =>
        Ok(store.Assets.Select(AssetDto));

    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(new
    {
        lastUpdated       = store.LastUpdated,
        dataSource        = store.DataSource,
        lastError         = store.LastError,
        nextUpdateSeconds = (int)Math.Max(0,
            (store.NextScheduledUpdate - DateTime.UtcNow).TotalSeconds),
    });

    // ── On-demand refresh ────────────────────────────────────────────────────

    /// <summary>
    /// Triggers an immediate fetch from Yahoo Finance (RapidAPI).
    /// Waits for the fetch to complete before responding so the client
    /// can reload all views with fresh data straight away.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshNow(CancellationToken ct)
    {
        var error = await updater.TriggerNowAsync(ct);
        return Ok(new
        {
            lastUpdated = store.LastUpdated,
            dataSource  = store.DataSource,
            lastError   = store.LastError,
            success     = store.LastError == null,
            message     = store.LastError ?? "Data updated successfully from Yahoo Finance.",
        });
    }

    // ── DTO ──────────────────────────────────────────────────────────────────

    private static object AssetDto(Asset a) => new
    {
        a.Symbol,
        a.Name,
        type               = a.Type.ToString(),
        a.CurrentPrice,
        a.PreviousClosePrice,
        dailyChangeAmount  = a.DailyChangeAmount,
        dailyChangePercent = a.DailyChangePercent,
        a.Week52High,
        a.Week52Low,
        marketCap          = a.MarketCap,
        volume             = a.Volume,
    };
}

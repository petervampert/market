using MarketAnalysis.Data;
using MarketAnalysis.Models;
using Microsoft.AspNetCore.Mvc;

namespace MarketAnalysis.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController(MarketDataStore store) : ControllerBase
{
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var p = store.Portfolio;
        return Ok(new
        {
            name               = p.Name,
            totalInvested      = p.TotalInvested,
            totalCurrentValue  = p.TotalCurrentValue,
            totalUnrealizedGain       = p.TotalUnrealizedGain,
            totalUnrealizedGainPercent= p.TotalUnrealizedGainPercent,
            totalDailyGain            = p.TotalDailyGain,
            totalDailyGainPercent     = p.TotalDailyGainPercent,
            holdings           = p.Holdings.Select(HoldingDto),
        });
    }

    [HttpGet("roi")]
    public IActionResult GetRoi() =>
        Ok(store.RoiResults.Select(r => new
        {
            r.Symbol,
            r.Name,
            r.TotalInvested,
            r.CurrentValue,
            r.UnrealizedGain,
            r.SimpleRoiPercent,
            r.AnnualisedRoiPercent,
            r.YearsHeld,
            holdingSince = r.HoldingSince,
        }));

    [HttpGet("allocation")]
    public IActionResult GetAllocation() => Ok(new
    {
        byType  = store.Allocation.ByType.Select(t => new
        {
            type    = t.Type.ToString(),
            t.Value,
            t.Percent,
        }),
        byAsset = store.Allocation.ByAsset.Select(a => new
        {
            a.Symbol,
            a.Name,
            a.Value,
            a.Percent,
        }),
    });

    [HttpGet("transactions")]
    public IActionResult GetTransactions() =>
        Ok(store.Transactions
            .OrderByDescending(t => t.Date)
            .Select(t => new
            {
                t.AssetSymbol,
                type      = t.Type.ToString(),
                t.Quantity,
                t.PricePerUnit,
                t.Fees,
                totalCost = t.TotalCost,
                date      = t.Date,
            }));

    private static object HoldingDto(Holding h) => new
    {
        h.AssetSymbol,
        h.AssetName,
        assetType             = h.AssetType.ToString(),
        h.Quantity,
        h.AverageBuyPrice,
        h.CurrentPrice,
        h.TotalInvested,
        h.CurrentValue,
        h.UnrealizedGain,
        h.UnrealizedGainPercent,
        h.DailyChangePercent,
        dailyGain             = h.DailyGain,
    };
}

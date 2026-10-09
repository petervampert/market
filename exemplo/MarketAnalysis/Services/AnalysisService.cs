using MarketAnalysis.Models;

namespace MarketAnalysis.Services;

public class AnalysisService
{
    /// <summary>
    /// Builds a Portfolio from a list of transactions and current asset prices.
    /// </summary>
    public Portfolio BuildPortfolio(string name, List<Transaction> transactions, List<Asset> assets)
    {
        var assetMap = assets.ToDictionary(a => a.Symbol);
        var holdingsMap = new Dictionary<string, (decimal qty, decimal totalCost)>();

        foreach (var tx in transactions.OrderBy(t => t.Date))
        {
            if (!holdingsMap.ContainsKey(tx.AssetSymbol))
                holdingsMap[tx.AssetSymbol] = (0, 0);

            var (qty, cost) = holdingsMap[tx.AssetSymbol];

            if (tx.Type == TransactionType.Buy)
            {
                holdingsMap[tx.AssetSymbol] = (qty + tx.Quantity, cost + tx.TotalCost);
            }
            else // Sell — reduce proportionally
            {
                decimal sellRatio = tx.Quantity / (qty == 0 ? 1 : qty);
                holdingsMap[tx.AssetSymbol] = (qty - tx.Quantity, cost - cost * sellRatio);
            }
        }

        var holdings = holdingsMap
            .Where(kv => kv.Value.qty > 0)
            .Select(kv =>
            {
                var symbol = kv.Key;
                var (qty, totalCost) = kv.Value;
                var asset = assetMap.GetValueOrDefault(symbol);

                return new Holding
                {
                    AssetSymbol = symbol,
                    AssetName = asset?.Name ?? symbol,
                    AssetType = asset?.Type ?? AssetType.Stock,
                    Quantity = qty,
                    AverageBuyPrice = qty == 0 ? 0 : totalCost / qty,
                    CurrentPrice = asset?.CurrentPrice ?? 0,
                    DailyChangePercent = asset?.DailyChangePercent ?? 0
                };
            })
            .ToList();

        return new Portfolio
        {
            Name = name,
            Holdings = holdings,
            Transactions = transactions
        };
    }

    /// <summary>
    /// Calculates annualised ROI for each holding.
    /// </summary>
    public List<RoiResult> CalculateRoi(Portfolio portfolio, List<Transaction> transactions)
    {
        var results = new List<RoiResult>();

        foreach (var holding in portfolio.Holdings)
        {
            var firstBuy = transactions
                .Where(t => t.AssetSymbol == holding.AssetSymbol && t.Type == TransactionType.Buy)
                .OrderBy(t => t.Date)
                .FirstOrDefault();

            double yearsHeld = firstBuy is null
                ? 1
                : Math.Max((DateTime.Now - firstBuy.Date).TotalDays / 365.0, 1.0 / 365);

            double simpleRoi = holding.TotalInvested == 0
                ? 0
                : (double)(holding.UnrealizedGain / holding.TotalInvested);

            // CAGR = (endValue / startValue)^(1/years) - 1
            double cagr = holding.TotalInvested == 0 ? 0
                : Math.Pow((double)(holding.CurrentValue / holding.TotalInvested), 1.0 / yearsHeld) - 1;

            results.Add(new RoiResult
            {
                Symbol = holding.AssetSymbol,
                Name = holding.AssetName,
                TotalInvested = holding.TotalInvested,
                CurrentValue = holding.CurrentValue,
                UnrealizedGain = holding.UnrealizedGain,
                SimpleRoiPercent = (decimal)(simpleRoi * 100),
                AnnualisedRoiPercent = (decimal)(cagr * 100),
                YearsHeld = (decimal)yearsHeld,
                HoldingSince = firstBuy?.Date ?? DateTime.Now
            });
        }

        return results.OrderByDescending(r => r.SimpleRoiPercent).ToList();
    }

    /// <summary>
    /// Computes portfolio allocation percentages by asset and by type.
    /// </summary>
    public AllocationReport GetAllocation(Portfolio portfolio)
    {
        var total = portfolio.TotalCurrentValue;

        var byAsset = portfolio.Holdings
            .Select(h => new AssetAllocation
            {
                Symbol = h.AssetSymbol,
                Name = h.AssetName,
                Value = h.CurrentValue,
                Percent = total == 0 ? 0 : h.CurrentValue / total * 100
            })
            .OrderByDescending(a => a.Percent)
            .ToList();

        var byType = portfolio.Holdings
            .GroupBy(h => h.AssetType)
            .Select(g => new TypeAllocation
            {
                Type = g.Key,
                Value = g.Sum(h => h.CurrentValue),
                Percent = total == 0 ? 0 : g.Sum(h => h.CurrentValue) / total * 100
            })
            .OrderByDescending(t => t.Percent)
            .ToList();

        return new AllocationReport { ByAsset = byAsset, ByType = byType };
    }

    /// <summary>
    /// Returns the top N gainers and losers from the asset list.
    /// </summary>
    public (List<Asset> Gainers, List<Asset> Losers) GetTopMovers(List<Asset> assets, int count = 5)
    {
        var gainers = assets.OrderByDescending(a => a.DailyChangePercent).Take(count).ToList();
        var losers = assets.OrderBy(a => a.DailyChangePercent).Take(count).ToList();
        return (gainers, losers);
    }
}

// --- Result types ---

public class RoiResult
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal UnrealizedGain { get; set; }
    public decimal SimpleRoiPercent { get; set; }
    public decimal AnnualisedRoiPercent { get; set; }
    public decimal YearsHeld { get; set; }
    public DateTime HoldingSince { get; set; }
}

public class AssetAllocation
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal Percent { get; set; }
}

public class TypeAllocation
{
    public AssetType Type { get; set; }
    public decimal Value { get; set; }
    public decimal Percent { get; set; }
}

public class AllocationReport
{
    public List<AssetAllocation> ByAsset { get; set; } = [];
    public List<TypeAllocation> ByType { get; set; } = [];
}

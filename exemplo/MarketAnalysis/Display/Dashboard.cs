using MarketAnalysis.Models;
using MarketAnalysis.Services;

namespace MarketAnalysis.Display;

public static class Dashboard
{
    // ── Colour helpers ───────────────────────────────────────────────────────
    private static void WriteColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    private static ConsoleColor ChangeColor(decimal value) =>
        value >= 0 ? ConsoleColor.Green : ConsoleColor.Red;

    private static string Arrow(decimal value) => value >= 0 ? "▲" : "▼";
    private static string Sign(decimal value)  => value >= 0 ? "+" : "";

    // ── Layout helpers ───────────────────────────────────────────────────────
    private static void PrintSeparator(char ch = '─', int width = 100)
        => Console.WriteLine(new string(ch, width));

    private static void PrintHeader(string title)
    {
        Console.WriteLine();
        PrintSeparator('═');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  {title.ToUpper()}");
        Console.ResetColor();
        PrintSeparator('═');
    }

    private static void PrintSubHeader(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  ── {title} ");
        Console.ResetColor();
        PrintSeparator();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  MARKET OVERVIEW
    // ════════════════════════════════════════════════════════════════════════
    public static void RenderMarketOverview(
        List<MarketSummary> indices,
        List<SectorPerformance> sectors,
        List<Asset> gainers,
        List<Asset> losers)
    {
        PrintHeader($"📊  Market Overview  —  {DateTime.Now:dddd, MMMM dd yyyy  HH:mm}");

        // Index row
        Console.Write("  ");
        foreach (var idx in indices)
        {
            Console.Write($"{idx.IndexName}: ");
            WriteColored($"{idx.Value:N2} {Arrow(idx.ChangePercent)} {Sign(idx.ChangePercent)}{idx.ChangePercent:F2}%  ",
                         ChangeColor(idx.ChangePercent));
        }
        Console.WriteLine("\n");

        // Sectors table
        PrintSubHeader("Sector Performance");
        Console.WriteLine($"  {"Sector",-22} {"Change",8}   {"Gainers",8}  {"Losers",8}  Bar");
        PrintSeparator('-');
        foreach (var s in sectors.OrderByDescending(x => x.ChangePercent))
        {
            Console.Write($"  {s.Sector,-22}");
            WriteColored($"  {Sign(s.ChangePercent)}{s.ChangePercent,6:F2}%", ChangeColor(s.ChangePercent));
            Console.Write($"   {s.GainersCount,4} up   {s.LosersCount,4} dn   ");
            RenderBar(s.ChangePercent, -3m, 3m, 20);
            Console.WriteLine();
        }

        // Top movers
        Console.WriteLine();
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"{"TOP GAINERS",-48}");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("TOP LOSERS");
        Console.ResetColor();
        Console.WriteLine($"  {"Symbol",-8} {"Price",10} {"Change %",10}    {"Symbol",-8} {"Price",10} {"Change %",10}");
        PrintSeparator('-');

        int rows = Math.Max(gainers.Count, losers.Count);
        for (int i = 0; i < rows; i++)
        {
            Console.Write("  ");
            if (i < gainers.Count)
            {
                var g = gainers[i];
                Console.Write($"{g.Symbol,-8} {g.CurrentPrice,10:N2}");
                WriteColored($" {Sign(g.DailyChangePercent)}{g.DailyChangePercent,8:F2}%", ConsoleColor.Green);
            }
            else Console.Write(new string(' ', 30));

            Console.Write("    ");

            if (i < losers.Count)
            {
                var l = losers[i];
                Console.Write($"{l.Symbol,-8} {l.CurrentPrice,10:N2}");
                WriteColored($" {Sign(l.DailyChangePercent)}{l.DailyChangePercent,8:F2}%", ConsoleColor.Red);
            }
            Console.WriteLine();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PORTFOLIO SUMMARY
    // ════════════════════════════════════════════════════════════════════════
    public static void RenderPortfolioSummary(Portfolio portfolio)
    {
        PrintHeader($"💼  Portfolio: {portfolio.Name}");

        var totalInvested   = portfolio.TotalInvested;
        var totalValue      = portfolio.TotalCurrentValue;
        var totalGain       = portfolio.TotalUnrealizedGain;
        var totalGainPct    = portfolio.TotalUnrealizedGainPercent;
        var dailyGain       = portfolio.TotalDailyGain;
        var dailyGainPct    = portfolio.TotalDailyGainPercent;

        // Summary cards
        Console.WriteLine();
        PrintCard("Total Invested",    $"$ {totalInvested:N2}",        ConsoleColor.White);
        PrintCard("Current Value",     $"$ {totalValue:N2}",           ConsoleColor.Cyan);
        PrintCard("Unrealised P/L",    $"{Sign(totalGain)}$ {totalGain:N2}  ({Sign(totalGainPct)}{totalGainPct:F2}%)", ChangeColor(totalGain));
        PrintCard("Today's P/L",       $"{Sign(dailyGain)}$ {dailyGain:N2}  ({Sign(dailyGainPct)}{dailyGainPct:F2}%)", ChangeColor(dailyGain));
        Console.WriteLine();

        // Holdings table
        PrintSubHeader("Holdings");
        Console.WriteLine($"  {"Symbol",-7} {"Name",-26} {"Type",-8} {"Qty",7} {"Avg Cost",11} {"Cur Price",11} {"Cur Value",12} {"Unreal. P/L",13} {"ROI %",8} {"Day %",8}");
        PrintSeparator('-');

        foreach (var h in portfolio.Holdings.OrderByDescending(x => x.CurrentValue))
        {
            Console.Write($"  {h.AssetSymbol,-7} {h.AssetName,-26} {h.AssetType,-8} {h.Quantity,7:N4} {h.AverageBuyPrice,11:N2} {h.CurrentPrice,11:N2} {h.CurrentValue,12:N2}");
            WriteColored($" {Sign(h.UnrealizedGain)}{h.UnrealizedGain,10:N2}", ChangeColor(h.UnrealizedGain));
            WriteColored($" {Sign(h.UnrealizedGainPercent)}{h.UnrealizedGainPercent,7:F2}%", ChangeColor(h.UnrealizedGainPercent));
            WriteColored($" {Sign(h.DailyChangePercent)}{h.DailyChangePercent,7:F2}%", ChangeColor(h.DailyChangePercent));
            Console.WriteLine();
        }

        PrintSeparator('-');
        Console.Write($"  {"TOTAL",-7} {"",-26} {"",-8} {"",7} {"",11} {"",11} {totalValue,12:N2}");
        WriteColored($" {Sign(totalGain)}{totalGain,10:N2}", ChangeColor(totalGain));
        WriteColored($" {Sign(totalGainPct)}{totalGainPct,7:F2}%", ChangeColor(totalGainPct));
        Console.WriteLine();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ROI BOARD
    // ════════════════════════════════════════════════════════════════════════
    public static void RenderRoiBoard(List<RoiResult> roiResults)
    {
        PrintHeader("📈  Return on Investment Board");
        Console.WriteLine($"  {"Symbol",-7} {"Name",-26} {"Invested",12} {"Cur Value",12} {"Gain / Loss",13} {"Simple ROI",11} {"Ann. ROI (CAGR)",16} {"Held (yrs)",11} {"Since",-12}");
        PrintSeparator('-');

        foreach (var r in roiResults)
        {
            Console.Write($"  {r.Symbol,-7} {r.Name,-26} {r.TotalInvested,12:N2} {r.CurrentValue,12:N2}");
            WriteColored($" {Sign(r.UnrealizedGain)}{r.UnrealizedGain,10:N2}", ChangeColor(r.UnrealizedGain));
            WriteColored($" {Sign(r.SimpleRoiPercent)}{r.SimpleRoiPercent,9:F2}%", ChangeColor(r.SimpleRoiPercent));
            WriteColored($"   {Sign(r.AnnualisedRoiPercent)}{r.AnnualisedRoiPercent,12:F2}%", ChangeColor(r.AnnualisedRoiPercent));
            Console.Write($"   {r.YearsHeld,8:F2}");
            Console.WriteLine($"   {r.HoldingSince:MMM yyyy}");
        }

        // Best / worst
        if (roiResults.Count > 0)
        {
            var best  = roiResults.First();
            var worst = roiResults.Last();
            Console.WriteLine();
            Console.Write("  Best performer: ");
            WriteColored($"{best.Symbol} ({Sign(best.SimpleRoiPercent)}{best.SimpleRoiPercent:F2}%)", ConsoleColor.Green);
            Console.Write("    Worst performer: ");
            WriteColored($"{worst.Symbol} ({Sign(worst.SimpleRoiPercent)}{worst.SimpleRoiPercent:F2}%)", ConsoleColor.Red);
            Console.WriteLine();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ALLOCATION CHART
    // ════════════════════════════════════════════════════════════════════════
    public static void RenderAllocation(AllocationReport report)
    {
        PrintHeader("🥧  Portfolio Allocation");

        PrintSubHeader("By Asset Type");
        foreach (var t in report.ByType)
        {
            Console.Write($"  {t.Type,-10}  $ {t.Value,12:N2}  {t.Percent,6:F1}%  ");
            RenderBar(t.Percent, 0, 100, 40);
            Console.WriteLine();
        }

        PrintSubHeader("By Asset (Top 10)");
        Console.WriteLine($"  {"Symbol",-7} {"Name",-26} {"Value",13} {"Weight",8}  Chart");
        PrintSeparator('-');
        foreach (var a in report.ByAsset.Take(10))
        {
            Console.Write($"  {a.Symbol,-7} {a.Name,-26} $ {a.Value,11:N2} {a.Percent,7:F1}%  ");
            RenderBar(a.Percent, 0, 100, 35);
            Console.WriteLine();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ASSET SCREENER  (all tracked assets)
    // ════════════════════════════════════════════════════════════════════════
    public static void RenderAssetScreener(List<Asset> assets)
    {
        PrintHeader("🔍  Asset Screener");
        Console.WriteLine($"  {"Symbol",-7} {"Name",-28} {"Type",-8} {"Price",12} {"Day Chg $",11} {"Day Chg %",10} {"52W High",11} {"52W Low",11} {"Volume",16}");
        PrintSeparator('-');

        foreach (var a in assets.OrderBy(x => x.Type).ThenByDescending(x => x.MarketCap))
        {
            Console.Write($"  {a.Symbol,-7} {a.Name,-28} {a.Type,-8} {a.CurrentPrice,12:N2}");
            WriteColored($" {Sign(a.DailyChangeAmount)}{a.DailyChangeAmount,9:N2}", ChangeColor(a.DailyChangeAmount));
            WriteColored($" {Sign(a.DailyChangePercent)}{a.DailyChangePercent,8:F2}%", ChangeColor(a.DailyChangePercent));
            Console.WriteLine($" {a.Week52High,11:N2} {a.Week52Low,11:N2} {a.Volume,16:N0}");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TRANSACTION HISTORY
    // ════════════════════════════════════════════════════════════════════════
    public static void RenderTransactionHistory(List<Transaction> transactions)
    {
        PrintHeader("🧾  Transaction History");
        Console.WriteLine($"  {"Date",-13} {"Type",-6} {"Symbol",-8} {"Qty",10} {"Price/Unit",12} {"Fees",8} {"Total",14}");
        PrintSeparator('-');

        foreach (var tx in transactions.OrderByDescending(t => t.Date))
        {
            Console.Write($"  {tx.Date:dd MMM yyyy}  ");
            var color = tx.Type == TransactionType.Buy ? ConsoleColor.Green : ConsoleColor.Red;
            WriteColored($"{tx.Type,-6}", color);
            Console.WriteLine($"  {tx.AssetSymbol,-8} {tx.Quantity,10:N4} {tx.PricePerUnit,12:N2} {tx.Fees,8:N2} {tx.TotalCost,14:N2}");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════════════
    private static void PrintCard(string label, string value, ConsoleColor color)
    {
        Console.Write($"  ┌─ {label} ");
        Console.WriteLine(new string('─', Math.Max(0, 28 - label.Length)) + "┐");
        Console.Write("  │  ");
        WriteColored($"{value,-28}", color);
        Console.WriteLine("│");
        Console.WriteLine("  └" + new string('─', 30) + "┘");
    }

    private static void RenderBar(decimal value, decimal min, decimal max, int width)
    {
        decimal range = max - min;
        int filled = range == 0 ? 0 : (int)Math.Round((value - min) / range * width);
        filled = Math.Clamp(filled, 0, width);

        // Mid-point (zero) bar for change values
        if (min < 0 && max > 0)
        {
            int zeroPos = (int)Math.Round(-min / range * width);
            int barLen  = (int)Math.Round((value - min) / range * width);
            barLen = Math.Clamp(barLen, 0, width);

            var buf = new char[width];
            Array.Fill(buf, ' ');
            buf[zeroPos] = '│';

            if (value >= 0)
            {
                for (int i = zeroPos; i < barLen; i++) buf[i] = '█';
                buf[zeroPos] = '│';
            }
            else
            {
                for (int i = barLen; i < zeroPos; i++) buf[i] = '▓';
            }

            Console.ForegroundColor = value >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write("[" + new string(buf) + "]");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[" + new string('█', filled) + new string('░', width - filled) + "]");
            Console.ResetColor();
        }
    }
}

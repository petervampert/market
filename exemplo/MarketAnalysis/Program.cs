using MarketAnalysis.Data;
using MarketAnalysis.Services;
using MarketAnalysis.Services.LiveData;

var builder = WebApplication.CreateBuilder(args);

// ── Core services ────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSingleton<AnalysisService>();
builder.Services.AddSingleton<MarketDataStore>();

// ── Live data fetcher  (YH Finance via RapidAPI) ─────────────────────────────
builder.Services.AddHttpClient("yhfinance", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MarketAnalysis/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IMarketDataFetcher, YhFinanceFetcher>();

// Register as singleton so the controller can inject it directly for TriggerNowAsync,
// and also register it as a hosted service so ASP.NET Core manages its lifetime.
builder.Services.AddSingleton<MarketUpdateService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MarketUpdateService>());

var app = builder.Build();

app.UseDefaultFiles();   // serves wwwroot/index.html
app.UseStaticFiles();    // serves wwwroot/**

app.MapControllers();

app.Run();

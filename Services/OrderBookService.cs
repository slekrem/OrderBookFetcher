using Microsoft.EntityFrameworkCore;
using OrderBookFetcher.Data;
using OrderBookFetcher.Models;
using Serilog;

namespace OrderBookFetcher.Services;

public class OrderBookService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;

    private readonly List<(string Exchange, string Url)> _endpoints = new()
    {
        ("Bitmex", "https://www.bitmex.com/api/v1/orderBook/L2?symbol=XBTUSD&depth=0"),
        ("Bybit", "https://api.bybit.com/v5/market/orderbook?category=linear&symbol=BTCUSDT&limit=10000"),
        ("Deribit", "https://www.deribit.com/api/v2/public/get_order_book?instrument_name=BTC-PERPETUAL&depth=10000"),
        ("Binance", "https://api.binance.com/api/v3/depth?symbol=BTCUSDT&limit=10000"),
        ("LNMarkets", "https://api.lnmarkets.com/v2/futures/ticker")
    };

    public OrderBookService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _httpClient = new HttpClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("OrderBookService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchAndStoreOrderBooksAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching order books.");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task FetchAndStoreOrderBooksAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderBookContext>();

        var tasks = _endpoints.Select(endpoint => FetchOrderBookAsync(endpoint.Exchange, endpoint.Url, dbContext));
        await Task.WhenAll(tasks);
        var driveInfo = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory));
        if (driveInfo.AvailableFreeSpace < 100 * 1024 * 1024) // 100 MB threshold
        {
            Log.Warning("Low disk space. Available: {AvailableFreeSpace} bytes", driveInfo.AvailableFreeSpace);
            return;
        }
        else
        {
            await dbContext.SaveChangesAsync();
            Log.Information("Order books fetched and stored at {Timestamp}", DateTime.UtcNow);
        }
    }

    private async Task FetchOrderBookAsync(string exchange, string url, OrderBookContext dbContext)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            var entry = new OrderBookEntry
            {
                Id = Guid.NewGuid(),
                Exchange = exchange,
                Url = url,
                TimestampUTC = DateTime.UtcNow,
                Result = result
            };

            await dbContext.OrderBookEntries.AddAsync(entry);
            Log.Information("Fetched {Exchange} order book from {Url}", exchange, url);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch {Exchange} order book from {Url}", exchange, url);
        }
    }

    public override void Dispose()
    {
        _httpClient.Dispose();
        base.Dispose();
    }
}

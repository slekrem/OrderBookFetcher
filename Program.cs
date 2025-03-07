using Microsoft.EntityFrameworkCore;
using OrderBookFetcher.Data;
using OrderBookFetcher.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/error.log", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<OrderBookContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<OrderBookService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderBookContext>();
    try
    {
        dbContext.Database.EnsureCreated();
        Log.Information("Datenbank wurde erfolgreich erstellt oder existiert bereits.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Fehler beim Erstellen der Datenbank.");
        throw;
    }
}

app.Run();

using Microsoft.EntityFrameworkCore;
using OrderBookFetcher.Models;

namespace OrderBookFetcher.Data;

public class OrderBookContext : DbContext
{
    public OrderBookContext(DbContextOptions<OrderBookContext> options) : base(options) { }

    public DbSet<OrderBookEntry> OrderBookEntries { get; set; }
}

namespace OrderBookFetcher.Models;

public class OrderBookEntry
{
    public Guid Id { get; set; }
    public string Exchange { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime TimestampUTC { get; set; }
    public string Result { get; set; } = string.Empty;
}

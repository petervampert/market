namespace MarketAnalysis.Models;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AssetSymbol { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public DateTime Date { get; set; }
    public decimal Fees { get; set; }

    public decimal TotalCost => Quantity * PricePerUnit + Fees;
}

public enum TransactionType
{
    Buy,
    Sell
}

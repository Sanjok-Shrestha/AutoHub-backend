namespace AutoHub.API.Models;

public class Transaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string PaymentStatus { get; set; } = "Paid";
    public DateTime? DueDate { get; set; }
    public ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();
}
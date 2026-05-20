namespace AutoHub.API.Models;

public class Invoice
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }      // Pre-discount
    public decimal FinalAmount { get; set; }      // Post-discount
    public bool IsDiscountApplied { get; set; } = false;

    public bool IsEmailSent { get; set; } = false;
    public DateTime Date { get; set; } = DateTime.UtcNow;  //  Use UtcNow for consistency

    public string? Status { get; set; } = "Paid";  // Optional: Paid, Pending, Refunded


    public List<InvoiceItem> Items { get; set; } = new();
}
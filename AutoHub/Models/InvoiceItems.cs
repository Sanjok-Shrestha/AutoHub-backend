namespace AutoHub.API.Models;

public class InvoiceItem
{
    public int Id { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }


    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }


    public int? PartId { get; set; }
    public Part? Part { get; set; }


    public string? PartNameForDisplay { get; set; }
}
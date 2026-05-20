public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }              
    public Sale Sale { get; set; } = null!;  
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

namespace VehicleManagementSystem.VehiclePartsAPI.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public bool IsDiscountApplied { get; set; } = false;
        public bool IsEmailSent { get; set; } = false;
        public DateTime Date { get; set; } = DateTime.Now;
        public List<InvoiceItem> Items { get; set; } = new();
    }
}
namespace VehicleManagementSystem.VehiclePartsAPI.DTOs
{
    public class CreateInvoiceDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public List<CreateInvoiceItemDto> Items { get; set; } = new();
    }

    public class CreateInvoiceItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
    }

    public class InvoiceResponseDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public bool IsDiscountApplied { get; set; }
        public bool IsEmailSent { get; set; }
        public DateTime Date { get; set; }
        public List<InvoiceItemResponseDto> Items { get; set; } = new();
    }

    public class InvoiceItemResponseDto
    {
        public string PartName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
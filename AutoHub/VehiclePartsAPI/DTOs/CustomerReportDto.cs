namespace VehicleManagementSystem.VehiclePartsAPI.DTOs
{
    public class CustomerSummaryDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int TotalInvoices { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastPurchaseDate { get; set; }
    }

    public class CustomerDetailedReportDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
        public IEnumerable<CustomerInvoiceDto> Invoices { get; set; } = new List<CustomerInvoiceDto>();
    }

    public class CustomerInvoiceDto
    {
        public int InvoiceId { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime Date { get; set; }
        public bool IsEmailSent { get; set; }
    }
}
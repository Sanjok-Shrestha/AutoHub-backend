namespace VehicleManagementSystem.VehiclePartsAPI.DTOs
{
    public class FinancialReportDto
    {
        public decimal TotalPurchaseSpend { get; set; }
        public decimal AverageInvoiceValue { get; set; }
        public decimal HighestPurchaseInvoice { get; set; }
        public string TopVendorName { get; set; }
        public decimal TopVendorSpend { get; set; }
        public int TotalQuantityPurchased { get; set; }
        public decimal StockValue { get; set; }
        public int ReportRecords { get; set; }
        public List<VendorSpendDto> VendorSpendBreakdown { get; set; }
        public List<ReportInvoiceDto> Invoices { get; set; }
    }
}
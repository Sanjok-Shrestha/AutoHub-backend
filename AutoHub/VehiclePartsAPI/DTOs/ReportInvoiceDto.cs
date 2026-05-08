namespace VehicleManagementSystem.VehiclePartsAPI.DTOs
{
    public class ReportInvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; }
        public string VendorName { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}
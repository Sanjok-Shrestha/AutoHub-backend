namespace VehicleManagementSystem.VehiclePartsAPI.DTOs
{
    public class PurchaseInvoiceDto
    {
        public string InvoiceNo { get; set; }

        public string VendorName { get; set; }

        public string PartName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public DateTime PurchaseDate { get; set; }

        public string Status { get; set; }
    }
}
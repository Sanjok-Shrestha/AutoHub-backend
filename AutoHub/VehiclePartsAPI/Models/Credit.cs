namespace VehicleManagementSystem.VehiclePartsAPI.Models
{
    public class Credit
    {
        public int Id { get; set; }

        public string CustomerName { get; set; }

        public string CustomerEmail { get; set; }

        public decimal Amount { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsPaid { get; set; } = false;

        // Optional (future integration)
        public int? SaleId { get; set; }
    }
}
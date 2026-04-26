namespace VehicleManagementSystem.VehiclePartsAPI.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }
}

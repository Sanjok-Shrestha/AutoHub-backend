using System;

namespace VehicleManagementSystem.VehiclePartsAPI.DTOs
{
    public class TopSpenderDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
    }

    public class RegularCustomerDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int PurchaseCount { get; set; }
    }

    public class OverdueCreditDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

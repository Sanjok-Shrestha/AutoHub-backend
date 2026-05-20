using AutoHub.API.Models;

public class ServiceHistory
{
    public int Id { get; set; }
    public int VehicleId { get; set; }

    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Status { get; set; } = "Completed";

    // Navigation Property
    public Vehicle? Vehicle { get; set; }
}
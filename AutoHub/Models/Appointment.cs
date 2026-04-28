namespace AutoHub.API.Models;

public class Appointment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime PreferredDate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
namespace AutoHub.API.Models;

public class PartRequest
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string PartName { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Submitted";
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
}
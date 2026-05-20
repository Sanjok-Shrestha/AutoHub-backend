
using System.ComponentModel.DataAnnotations;

namespace AutoHub.API.Models;

public class Vehicle
{
    public int Id { get; set; }


    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

  
    public String Year { get; set; } = string.Empty;  

    public string LicensePlate { get; set; } = string.Empty;
    public string? VIN { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ServiceHistory> ServiceHistories { get; set; } = new();

    public string VehicleNumber => LicensePlate;
}
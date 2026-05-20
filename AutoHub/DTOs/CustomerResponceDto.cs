
namespace AutoHub.DTOs;

public class CustomerResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }

    
    public DateTime RegisteredDate { get; set; }

    public List<VehicleDto> Vehicles { get; set; } = new();
}
public class ServiceHistoryDto
{
    public int Id { get; set; }
    public DateTime ServiceDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Status { get; set; } = string.Empty;
}

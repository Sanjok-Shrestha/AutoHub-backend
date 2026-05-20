
using System.ComponentModel.DataAnnotations;


namespace AutoHub.DTOs; 


public class VehicleDto
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string Make { get; set; } = string.Empty;
    [Required, StringLength(50)] public string Model { get; set; } = string.Empty;


    public String Year { get; set; } = string.Empty;  

    [Required, StringLength(50)] public string LicensePlate { get; set; } = string.Empty;
    public string? VIN { get; set; }
    public List<ServiceHistoryDto> ServiceHistories { get; set; } = new();
}


public class AppointmentDto
{
    [Required] public DateTime PreferredDate { get; set; }
    [Required, StringLength(100)] public string ServiceType { get; set; } = string.Empty;
    [StringLength(500)] public string? Notes { get; set; }
}

public class PartRequestDto
{
    [Required, StringLength(200)] public string PartName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string VehicleModel { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
}

public class ReviewDto
{
    [Required, Range(1, 5)] public int Rating { get; set; }
    [StringLength(500)] public string? Comment { get; set; }
    public int? AppointmentId { get; set; }
}


public class HistoryResponse
{
    public List<Sale> Purchases { get; set; } = new();
    public List<Appointment> Services { get; set; } = new();
}

public class ProfileResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }  
    public decimal TotalSpent { get; set; }
    public bool EmailConfirmed { get; set; }
}

public class CheckoutDto
{
    public string PaymentMethod { get; set; } = "Credit Card";
    public List<CheckoutItemDto> Items { get; set; } = new();
}

public class CheckoutItemDto
{
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
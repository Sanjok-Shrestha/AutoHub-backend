using System.ComponentModel.DataAnnotations;

namespace AutoHub.API.Models;

// For user registration (password required)
public class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(10)]
    public string Phone { get; set; } = string.Empty;

    //  Password is OPTIONAL - no [Required], nullable type
    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }
}
public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
}

public class ResendConfirmationDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}


public class VehicleDto
{
    [Required, StringLength(50)] public string Make { get; set; } = string.Empty;
    [Required, StringLength(50)] public string Model { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Year { get; set; } = string.Empty;
    [Required, StringLength(50)] public string LicensePlate { get; set; } = string.Empty;
    public string? VIN { get; set; }
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
    public List<Transaction> Purchases { get; set; } = new();
    public List<Appointment> Services { get; set; } = new();
}

public class ProfileResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; }
    public decimal TotalSpent { get; set; }
    public bool EmailConfirmed { get; set; }
}
// Purchase/Checkout DTOs
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
using System.ComponentModel.DataAnnotations;

namespace AutoHub.DTOs; 


public class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be 6-100 characters")]
    public string? Password { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be 6-100 characters")]
    public string Password { get; set; } = string.Empty;
}

public class ResendConfirmationDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ConfirmEmailDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;


    public int CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

 
    public string UserType { get; set; } = "Customer";  

    public int ExpiresIn { get; set; } = 3600;
}
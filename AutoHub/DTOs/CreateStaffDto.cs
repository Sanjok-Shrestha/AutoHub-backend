using System.ComponentModel.DataAnnotations;

namespace AutoHub.DTOs;

public class CreateStaffDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [StringLength(50)]
    public string Role { get; set; } = "Staff"; 
}
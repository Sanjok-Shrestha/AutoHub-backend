
using System.ComponentModel.DataAnnotations;

namespace AutoHub.API.Models;

public class Staff
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;


    [Required, StringLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(50)]
    public string Role { get; set; } = "Staff";


    [StringLength(20)]
    public string UserType { get; set; } = "Staff"; 

    [StringLength(20)]
    public string Status { get; set; } = "Active";

    [StringLength(500)]
    public string? Photo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
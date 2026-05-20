using System.ComponentModel.DataAnnotations;

namespace AutoHub.DTOs
{
    public class StaffDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = "Staff"; // "Staff", "Technician", "Manager", "Admin"

        [StringLength(20)]
        public string Status { get; set; } = "Active"; // "Active", "Inactive"

        [StringLength(500)]
        public string? Photo { get; set; } // Base64 string or URL

        // 🔐 Auth field - Required for creating login account
        [StringLength(100, MinimumLength = 8)]
        public string? Password { get; set; } // Required when creating staff with login access
    }
}   
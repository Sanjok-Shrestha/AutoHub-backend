using AutoHub.API.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }  // ✅ Must match FK in DB

    [Required]
    public DateTime PreferredDate { get; set; }  // ✅ PostgreSQL: timestamp with time zone

    [Required, StringLength(100)]  // ✅ Match DB column length
    public string ServiceType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    // ✅ Optional: Link to vehicle (if you added this)
    public int? VehicleId { get; set; }

    // Navigation properties (add [JsonIgnore] if causing circular refs)
    [JsonIgnore]
    public virtual Customer Customer { get; set; } = null!;

    [JsonIgnore]
    public virtual Vehicle? Vehicle { get; set; }
}
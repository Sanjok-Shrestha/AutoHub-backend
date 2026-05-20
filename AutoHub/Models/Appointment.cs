using AutoHub.API.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; } 

    [Required]
    public DateTime PreferredDate { get; set; }  

    [Required, StringLength(100)]  
    public string ServiceType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public int? VehicleId { get; set; }

    [JsonIgnore]
    public virtual Customer Customer { get; set; } = null!;

    [JsonIgnore]
    public virtual Vehicle? Vehicle { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;

namespace AutoHub.API.Models;


public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string? EmailConfirmationToken { get; set; }
    public DateTime? TokenExpiry { get; set; }

    public bool EmailConfirmed { get; set; } = false;
    public decimal TotalSpent { get; set; }
    public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = "Customer";


    public string UserType { get; set; } = "Customer";


    public decimal CreditLimit { get; set; } = 50.00m;

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
namespace AutoHub.API.Models;

public class SystemNotification
{
    public int Id { get; set; }
    public string Type { get; set; } = "LowStock";  // LowStock, SystemAlert, etc.
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
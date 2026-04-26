using VehicleManagementSystem.VehiclePartsAPI.Data;
using VehicleManagementSystem.VehiclePartsAPI.Models;

public class NotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    
    //low stock
    public List<Part> GetLowStockParts()
    {
        return _context.Parts
            .Where(p => p.Quantity < 10)
            .ToList();
    }

    public bool HasLowStock()
    {
        return _context.Parts.Any(p => p.Quantity < 10);
    }

    //Overdue credits (>1 month unpaid)
    public List<Credit> GetOverdueCredits()
    {
        return _context.Credits
            .Where(c => !c.IsPaid &&
                        c.CreatedDate < DateTime.UtcNow.AddMonths(-1))
            .ToList();
    }
}
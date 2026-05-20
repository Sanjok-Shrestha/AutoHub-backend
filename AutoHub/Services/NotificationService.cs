
using Microsoft.EntityFrameworkCore;
using AutoHub.API.Data;
using AutoHub.API.Models;

namespace AutoHub.API.Services;

public class NotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get parts with low stock (< 10 units)
    /// </summary>
    public async Task<List<Part>> GetLowStockPartsAsync(int threshold = 10)
    {
        return await _context.Parts
            .Where(p => p.Quantity < threshold)
            .OrderBy(p => p.Quantity)
            .ToListAsync();
    }

    /// <summary>
    /// Check if any parts have low stock
    /// </summary>
    public async Task<bool> HasLowStockAsync(int threshold = 10)
    {
        return await _context.Parts.AnyAsync(p => p.Quantity < threshold);
    }

    /// <summary>
    /// Get overdue credits (>30 days unpaid) for a SPECIFIC customer
    /// </summary>
    public async Task<List<Credit>> GetOverdueCreditsAsync(int customerId, int daysOverdue = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOverdue);

        return await _context.Credits
            .Where(c => c.CustomerId == customerId  
                     && !c.IsPaid
                     && c.CreatedDate < cutoffDate)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Get overdue credits for Admin (all customers)
    /// </summary>
    public async Task<List<Credit>> GetAllOverdueCreditsAsync(int daysOverdue = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOverdue);

        return await _context.Credits
            .Where(c => !c.IsPaid && c.CreatedDate < cutoffDate)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Mark credit reminder as sent (to prevent duplicate emails)
    /// </summary>
    public async Task<bool> MarkReminderSentAsync(int creditId)
    {
        var credit = await _context.Credits.FindAsync(creditId);
        if (credit == null || credit.IsPaid) return false;

        credit.LastReminderSent = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
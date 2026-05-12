using Microsoft.EntityFrameworkCore;
using VehicleManagementSystem.VehiclePartsAPI.Data;
using VehicleManagementSystem.VehiclePartsAPI.DTOs;

namespace VehicleManagementSystem.VehiclePartsAPI.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        // Top Spenders
        public async Task<List<TopSpenderDto>> GetTopSpendersAsync()
        {
            return await _context.Invoices
                .GroupBy(i => i.CustomerEmail) 
                .Select(g => new TopSpenderDto
                {
                    CustomerName = g.First().CustomerName,
                    CustomerEmail = g.Key,
                    TotalSpent = g.Sum(i => i.FinalAmount)
                })
                .OrderByDescending(r => r.TotalSpent)
                .ToListAsync();
        }

        //Regular Customers
        public async Task<List<RegularCustomerDto>> GetRegularCustomersAsync()
        {
            return await _context.Invoices
                .GroupBy(i => i.CustomerEmail)
                .Select(g => new RegularCustomerDto
                {
                    CustomerName = g.First().CustomerName,
                    CustomerEmail = g.Key,
                    PurchaseCount = g.Count()
                })
                .OrderByDescending(r => r.PurchaseCount)
                .ToListAsync();
        }

        // Overdue Credits
        public async Task<List<OverdueCreditDto>> GetOverdueCreditsAsync()
        {
            // Overdue means older than 1 month and unpaid
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            return await _context.Credits
                .Where(c => !c.IsPaid && c.CreatedDate < oneMonthAgo)
                .Select(c => new OverdueCreditDto
                {
                    CustomerName = c.CustomerName,
                    CustomerEmail = c.CustomerEmail,
                    Amount = c.Amount,
                    CreatedDate = c.CreatedDate
                })
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementSystem.VehiclePartsAPI.Data;
using VehicleManagementSystem.VehiclePartsAPI.DTOs;

namespace VehicleManagementSystem.VehiclePartsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerReportsController(AppDbContext context)
        {
            _context = context;
        }

        // Generates a report showing all customers and their total spendings
        [HttpGet("summary")]
        public async Task<ActionResult<IEnumerable<CustomerSummaryDto>>> GetCustomerSummaryReport()
        {
            var report = await _context.Invoices
                .GroupBy(i => new { i.CustomerName, i.CustomerEmail })
                .Select(g => new CustomerSummaryDto
                {
                    CustomerName = g.Key.CustomerName,
                    CustomerEmail = g.Key.CustomerEmail,
                    TotalInvoices = g.Count(),
                    TotalSpent = g.Sum(i => i.FinalAmount),
                    LastPurchaseDate = g.Max(i => i.Date)
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToListAsync();

            return Ok(report);
        }

        // Generates a detailed report of all invoices for a specific customer
        [HttpGet("details")]
        public async Task<ActionResult<CustomerDetailedReportDto>> GetCustomerDetailedReport([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Customer email is required.");
            }

            var invoices = await _context.Invoices
                .Where(i => i.CustomerEmail.ToLower() == email.ToLower())
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            if (!invoices.Any())
            {
                return NotFound($"No records found for customer with email: {email}");
            }

            var report = new CustomerDetailedReportDto
            {
                CustomerName = invoices.First().CustomerName,
                CustomerEmail = invoices.First().CustomerEmail,
                TotalSpent = invoices.Sum(i => i.FinalAmount),
                Invoices = invoices.Select(i => new CustomerInvoiceDto
                {
                    InvoiceId = i.Id,
                    FinalAmount = i.FinalAmount,
                    Date = i.Date,
                    IsEmailSent = i.IsEmailSent
                }).ToList()
            };

            return Ok(report);
        }

        
        // Generates a report showing the Top N customers by total spent
        [HttpGet("top-spenders")]
        public async Task<ActionResult<IEnumerable<CustomerSummaryDto>>> GetTopSpenders([FromQuery] int count = 5)
        {
            var report = await _context.Invoices
                .GroupBy(i => new { i.CustomerName, i.CustomerEmail })
                .Select(g => new CustomerSummaryDto
                {
                    CustomerName = g.Key.CustomerName,
                    CustomerEmail = g.Key.CustomerEmail,
                    TotalInvoices = g.Count(),
                    TotalSpent = g.Sum(i => i.FinalAmount),
                    LastPurchaseDate = g.Max(i => i.Date)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(count)
                .ToListAsync();

            return Ok(report);
        }
    }
}
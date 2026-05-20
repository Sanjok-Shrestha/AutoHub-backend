using AutoHub.API.Data;
using AutoHub.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace VehicleManagementSystem.VehiclePartsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("financial")]
        public async Task<ActionResult<FinancialReportDto>> GetFinancialReport(
            string period = "monthly",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var today = DateTime.Today;

            var invoices = await _context.PurchaseInvoices.ToListAsync();

            if (startDate.HasValue && endDate.HasValue)
            {
                invoices = invoices
                    .Where(i => i.PurchaseDate.Date >= startDate.Value.Date &&
                                i.PurchaseDate.Date <= endDate.Value.Date)
                    .ToList();
            }
            else if (period == "daily")
            {
                invoices = invoices
                    .Where(i => i.PurchaseDate.Date == today)
                    .ToList();
            }
            else if (period == "weekly")
            {
                var sevenDaysAgo = today.AddDays(-7);

                invoices = invoices
                    .Where(i => i.PurchaseDate.Date >= sevenDaysAgo &&
                                i.PurchaseDate.Date <= today)
                    .ToList();
            }
            else if (period == "monthly")
            {
                invoices = invoices
                    .Where(i => i.PurchaseDate.Month == today.Month &&
                                i.PurchaseDate.Year == today.Year)
                    .ToList();
            }
            else if (period == "yearly")
            {
                invoices = invoices
                    .Where(i => i.PurchaseDate.Year == today.Year)
                    .ToList();
            }

            var totalPurchaseSpend = invoices.Sum(i => i.TotalAmount);
            var averageInvoiceValue = invoices.Count > 0
                ? Math.Round(totalPurchaseSpend / invoices.Count)
                : 0;

            var highestPurchaseInvoice = invoices.Count > 0
                ? invoices.Max(i => i.TotalAmount)
                : 0;

            var vendorSpend = invoices
                .GroupBy(i => i.VendorName)
                .Select(g => new VendorSpendDto
                {
                    VendorName = g.Key,
                    Amount = g.Sum(i => i.TotalAmount)
                })
                .OrderByDescending(v => v.Amount)
                .ToList();

            var topVendor = vendorSpend.FirstOrDefault();

            var report = new FinancialReportDto
            {
                TotalPurchaseSpend = totalPurchaseSpend,
                AverageInvoiceValue = averageInvoiceValue,
                HighestPurchaseInvoice = highestPurchaseInvoice,
                TopVendorName = topVendor != null ? topVendor.VendorName : "-",
                TopVendorSpend = topVendor != null ? topVendor.Amount : 0,
                TotalQuantityPurchased = invoices.Sum(i => i.Quantity),
                StockValue = totalPurchaseSpend,
                ReportRecords = invoices.Count,
                VendorSpendBreakdown = vendorSpend,
                Invoices = invoices.Select(i => new ReportInvoiceDto
                {
                    Id = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    VendorName = i.VendorName,
                    PartName = i.PartName,
                    Quantity = i.Quantity,
                    TotalAmount = i.TotalAmount,
                    PurchaseDate = i.PurchaseDate
                }).ToList()
            };

            return Ok(report);
        }
    }
}
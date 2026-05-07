using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementSystem.VehiclePartsAPI.Data;
using VehicleManagementSystem.VehiclePartsAPI.DTOs;
using VehicleManagementSystem.VehiclePartsAPI.Models;

namespace VehicleManagementSystem.VehiclePartsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseInvoiceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchaseInvoiceController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseInvoice>>> GetPurchaseInvoices()
        {
            return await _context.PurchaseInvoices.ToListAsync();
        }

        
        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseInvoice>> GetPurchaseInvoice(int id)
        {
            var invoice = await _context.PurchaseInvoices.FindAsync(id);

            if (invoice == null)
            {
                return NotFound();
            }

            return invoice;
        }

        
        [HttpPost]
        public async Task<ActionResult<PurchaseInvoice>> CreatePurchaseInvoice(PurchaseInvoiceDto dto)
        {
            var invoice = new PurchaseInvoice
            {
                InvoiceNo = dto.InvoiceNo,
                VendorName = dto.VendorName,
                PartName = dto.PartName,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                TotalAmount = dto.Quantity * dto.UnitPrice,
                PurchaseDate = dto.PurchaseDate,
                Status = dto.Status
            };

            _context.PurchaseInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPurchaseInvoice), new { id = invoice.Id }, invoice);
        }

        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePurchaseInvoice(int id, PurchaseInvoiceDto dto)
        {
            var invoice = await _context.PurchaseInvoices.FindAsync(id);

            if (invoice == null)
            {
                return NotFound();
            }

            invoice.InvoiceNo = dto.InvoiceNo;
            invoice.VendorName = dto.VendorName;
            invoice.PartName = dto.PartName;
            invoice.Quantity = dto.Quantity;
            invoice.UnitPrice = dto.UnitPrice;
            invoice.TotalAmount = dto.Quantity * dto.UnitPrice;
            invoice.PurchaseDate = dto.PurchaseDate;
            invoice.Status = dto.Status;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseInvoice(int id)
        {
            var invoice = await _context.PurchaseInvoices.FindAsync(id);

            if (invoice == null)
            {
                return NotFound();
            }

            _context.PurchaseInvoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
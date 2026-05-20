using AutoHub.API.Data;
using AutoHub.API.Models;
using AutoHub.DTOs;
using Microsoft.EntityFrameworkCore;


namespace AutoHub.API.Services
{
    public class InvoiceService
    {
        private readonly AppDbContext _context;

        public InvoiceService(AppDbContext context)
        {
            _context = context;
        }

        // Get all invoices
        public async Task<List<InvoiceResponseDto>> GetAllAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Items)
                    .ThenInclude(i => i.Part)
                .ToListAsync();

            var result = new List<InvoiceResponseDto>();
            foreach (var invoice in invoices)
            {
                result.Add(MapToResponse(invoice));
            }
            return result;
        }

        // Get one invoice by ID
        public async Task<InvoiceResponseDto?> GetByIdAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                    .ThenInclude(i => i.Part)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return null;

            return MapToResponse(invoice);
        }

        // Create new invoice
        public async Task<InvoiceResponseDto> CreateInvoiceAsync(CreateInvoiceDto dto)
        {
            var items = new List<InvoiceItem>();
            decimal total = 0;

            foreach (var itemDto in dto.Items)
            {
                var part = await _context.Parts.FindAsync(itemDto.PartId);

                if (part == null)
                    throw new Exception($"Part {itemDto.PartId} not found");

                if (part.Quantity < itemDto.Quantity)
                    throw new Exception($"Not enough stock for {part.Name}. Available: {part.Quantity}");

                decimal subtotal = part.Price * itemDto.Quantity;
                total += subtotal;

                items.Add(new InvoiceItem
                {
                    PartId = itemDto.PartId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = part.Price,
                    Subtotal = subtotal
                });

                // Reduces stock
                part.Quantity -= itemDto.Quantity;
            }

            //  Applies 10% discount if total > 5000
            bool discountApplied = false;
            decimal finalAmount = total;
            if (total > 5000)
            {
                finalAmount = total * 0.90m;
                discountApplied = true;
            }

            //  Creates and save invoice
            var invoice = new Invoice
            {
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                TotalAmount = total,
                FinalAmount = finalAmount,
                IsDiscountApplied = discountApplied,
                Date = DateTime.UtcNow,
                Items = items
            };

            try
            {
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }

            return MapToResponse(invoice);
        }

        // Marks email as sent
        public async Task MarkEmailSentAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice != null)
            {
                invoice.IsEmailSent = true;
                await _context.SaveChangesAsync();
            }
        }

        // Updates basic invoice details
        public async Task<InvoiceResponseDto?> UpdateInvoiceAsync(int id, string customerName, string customerEmail)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                    .ThenInclude(i => i.Part)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return null;

            invoice.CustomerName = customerName;
            invoice.CustomerEmail = customerEmail;

            await _context.SaveChangesAsync();

            return MapToResponse(invoice);
        }

        // Deletes invoice and restores stock
        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                    .ThenInclude(i => i.Part)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return false;

            // Restore the part stock quantities
            foreach (var item in invoice.Items)
            {
                if (item.Part != null)
                {
                    item.Part.Quantity += item.Quantity;
                }
            }

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return true;
        }

        // Convert Invoice to InvoiceResponseDto
        private static InvoiceResponseDto MapToResponse(Invoice invoice)
        {
            var items = new List<InvoiceItemResponseDto>();
            foreach (var item in invoice.Items)
            {
                items.Add(new InvoiceItemResponseDto
                {
                    PartName = item.Part?.Name ?? "",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Subtotal = item.Subtotal
                });
            }

            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                CustomerName = invoice.CustomerName,
                CustomerEmail = invoice.CustomerEmail,
                TotalAmount = invoice.TotalAmount,
                FinalAmount = invoice.FinalAmount,
                IsDiscountApplied = invoice.IsDiscountApplied,
                IsEmailSent = invoice.IsEmailSent,
                Date = invoice.Date,
                Items = items
            };
        }
    }
}
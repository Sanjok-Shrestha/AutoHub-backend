using AutoHub.API.Services;
using AutoHub.DTOs;
using Microsoft.AspNetCore.Mvc;


namespace AutoHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly InvoiceService _invoiceService;
        private readonly InvoiceEmailService _emailService;

        public InvoiceController(InvoiceService invoiceService, InvoiceEmailService emailService)
        {
            _invoiceService = invoiceService;
            _emailService = emailService;
        }

        // gets all invoices
        [HttpGet]
        public async Task<IActionResult> GetAllInvoices()
        {
            var invoices = await _invoiceService.GetAllAsync();
            return Ok(invoices);
        }

        // gets one invoice by ID   
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
                return NotFound(new { message = $"Invoice {id} not found" });
            return Ok(invoice);
        }

        // Posts a new invoice
        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto)
        {
            try
            {
                var invoice = await _invoiceService.CreateInvoiceAsync(dto);
                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // posts an email to the customer with the invoice details
        [HttpPost("{id}/send-email")]
        public async Task<IActionResult> SendEmail(int id)
        {
            try
            {
                var invoice = await _invoiceService.GetByIdAsync(id);
                if (invoice == null)
                    return NotFound(new { message = $"Invoice {id} not found" });

                await _emailService.SendInvoiceEmailAsync(invoice);
                await _invoiceService.MarkEmailSentAsync(id);

                return Ok(new { message = "Email sent successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Updates an existing invoice's customer details
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, [FromBody] UpdateInvoiceDto dto)
        {
            try
            {
                var updatedInvoice = await _invoiceService.UpdateInvoiceAsync(id, dto.CustomerName, dto.CustomerEmail);
                if (updatedInvoice == null)
                    return NotFound(new { message = $"Invoice {id} not found" });

                return Ok(updatedInvoice);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Deletes an invoice and restores stock
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                var success = await _invoiceService.DeleteInvoiceAsync(id);
                if (!success)
                    return NotFound(new { message = $"Invoice {id} not found" });

                return Ok(new { message = $"Invoice {id} deleted successfully and part stock restored." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoHub.API.Data;
using AutoHub.API.Models;

namespace AutoHub.API.Controllers;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "Admin,Staff")]
public class AdminPaymentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminPaymentsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Get all pending payment verifications
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingPayments()
    {
        var pending = await _db.Transactions
            .Include(t => t.Customer)
            .Where(t => t.PaymentStatus == "Pending Verification")
            .OrderByDescending(t => t.Date)
            .Select(t => new
            {
                t.Id,
                t.TotalAmount,
                t.PaymentMethod,
                t.PaymentStatus,
                t.Date,
                CustomerName = t.Customer.Name,
                CustomerEmail = t.Customer.Email
            })
            .ToListAsync();

        return Ok(pending);
    }

    /// <summary>
    /// Approve a pending payment & update customer TotalSpent
    /// </summary>
    [HttpPost("verify/{id}")]
    public async Task<IActionResult> VerifyPayment(int id)
    {
        var transaction = await _db.Transactions
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null) return NotFound(new { error = "Transaction not found" });
        if (transaction.PaymentStatus != "Pending Verification")
            return BadRequest(new { error = "Payment is not pending verification" });

        transaction.PaymentStatus = "Paid";
        transaction.Date = DateTime.UtcNow;

        if (transaction.Customer != null)
            transaction.Customer.TotalSpent += transaction.TotalAmount;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Payment verified successfully", transactionId = transaction.Id });
    }

    /// <summary>
    /// Reject a pending payment
    /// </summary>
    [HttpPost("reject/{id}")]
    public async Task<IActionResult> RejectPayment(int id)
    {
        var transaction = await _db.Transactions.FindAsync(id);

        if (transaction == null) return NotFound(new { error = "Transaction not found" });
        if (transaction.PaymentStatus != "Pending Verification")
            return BadRequest(new { error = "Payment is not pending verification" });

        transaction.PaymentStatus = "Payment Failed";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Payment rejected", transactionId = transaction.Id });
    }
}
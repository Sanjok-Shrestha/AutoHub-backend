using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoHub.API.Data;

namespace AutoHub.API.Controllers;

[ApiController, Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db) => _db = db;

    [HttpPost("khalti/webhook")]
    public async Task<IActionResult> KhaltiWebhook([FromBody] KhaltiWebhookPayload? payload)
    {
        if (payload?.status != "Completed") return BadRequest(new { error = "Payment not completed" });
        if (!int.TryParse(payload.purchase_order_id, out int tid)) return BadRequest(new { error = "Invalid ID" });

        var tx = await _db.Sales.Include(t => t.Customer).FirstOrDefaultAsync(t => t.Id == tid);
        if (tx == null || tx.PaymentMethod != "Khalti" || tx.PaymentStatus == "Paid")
            return Ok(new { message = "Already verified or invalid" });

        tx.PaymentStatus = "Paid";
        tx.Date = DateTime.UtcNow;
        if (tx.Customer != null) tx.Customer.TotalSpent += tx.TotalAmount;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Payment verified", transactionId = tid });
    }
}

public class KhaltiWebhookPayload
{
    public string pidx { get; set; } = string.Empty;
    public string purchase_order_id { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoHub.API.Services;
using AutoHub.API.Models;

namespace AutoHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]

public class NotificationsController : ControllerBase 
{
    private readonly NotificationService _service;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        NotificationService service,
        ILogger<NotificationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ✅ Helper: Get current customer ID from JWT
    private int? GetCurrentCustomerId()
    {
        var idStr = User.FindFirst("CustomerId")?.Value;
        return string.IsNullOrEmpty(idStr) ? null : int.Parse(idStr);
    }

    /// <summary>
    /// Get low stock parts
    /// </summary>
    [HttpGet("low-stock")]
  
    public async Task<ActionResult<IEnumerable<Part>>> GetLowStockParts(
        [FromQuery] int threshold = 10)
    {
        try
        {
            var parts = await _service.GetLowStockPartsAsync(threshold);
            return Ok(parts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching low stock parts");
            return StatusCode(500, new { error = "Unable to retrieve low stock notifications" });
        }
    }

    /// <summary>
    /// Check if any parts have low stock (Admin/Staff only)
    /// </summary>
    [HttpGet("has-low-stock")]
  
    public async Task<ActionResult<bool>> HasLowStock([FromQuery] int threshold = 10)
    {
        try
        {
            var hasLowStock = await _service.HasLowStockAsync(threshold);
            return Ok(hasLowStock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking low stock status");
            return StatusCode(500, new { error = "Unable to check stock status" });
        }
    }

    /// <summary>
    ///  Get overdue credits ONLY for current customer (or all for Admin)
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<Credit>>> GetOverdueCredits(
        [FromQuery] int daysOverdue = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var currentCustomerId = GetCurrentCustomerId();
            var isAdmin = User.IsInRole("Admin");

            List<Credit> credits;

            if (isAdmin)
            {
                //  Admin: see all overdue credits (with pagination)
                credits = await _service.GetAllOverdueCreditsAsync(daysOverdue);
            }
            else if (currentCustomerId.HasValue)
            {
                //  Customer: see ONLY their own overdue credits
                credits = await _service.GetOverdueCreditsAsync(
                    currentCustomerId.Value, daysOverdue);
            }
            else
            {
                //  Walk-in/unauthenticated: return empty (shouldn't happen due to [Authorize])
                return Ok(new List<Credit>());
            }

            // Apply pagination
            var pagedCredits = credits
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Add pagination header
            Response.Headers.Add("X-Pagination",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    page,
                    pageSize,
                    totalItems = credits.Count,
                    totalPages = (int)Math.Ceiling(credits.Count / (double)pageSize)
                }));

            return Ok(pagedCredits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching overdue credits");
            return StatusCode(500, new { error = "Unable to retrieve overdue credit notifications" });
        }
    }

    /// <summary>
    /// Mark a credit reminder as sent (prevent duplicate emails)
    /// </summary>
    [HttpPost("credits/{id}/reminder-sent")]
 
    public async Task<IActionResult> MarkReminderSent(int id)
    {
        try
        {
            var success = await _service.MarkReminderSentAsync(id);
            if (!success)
                return NotFound(new { error = "Credit not found or already paid" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking reminder sent for credit {CreditId}", id);
            return StatusCode(500, new { error = "Unable to update reminder status" });
        }
    }
}
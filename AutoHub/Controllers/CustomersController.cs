using AutoHub.API.Data;
using AutoHub.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.API.Controllers;

[ApiController, Route("api/customers"), Authorize(Roles = "Customer")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<Customer> _hasher = new();

    public CustomersController(AppDbContext db) => _db = db;

    private int GetId() => int.Parse(User.FindFirst("CustomerId")?.Value ?? throw new UnauthorizedAccessException());

    #region Feature 12: Profile & Vehicles
    [HttpGet("profile")]
    public async Task<ActionResult<ProfileResponse>> GetProfile()
    {
        var customerId = GetId();

        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == customerId);
        if (c == null) return NotFound(new { error = "Not found" });

        // ✅ Calculate total spent from PAID transactions only
        var totalSpent = await _db.Transactions
            .Where(t => t.CustomerId == customerId && t.PaymentStatus == "Paid")
            .SumAsync(t => t.TotalAmount);

        return Ok(new ProfileResponse
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            RegisteredDate = c.RegisteredDate,
            TotalSpent = totalSpent,  // ✅ Now shows actual calculated total
            EmailConfirmed = c.EmailConfirmed
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var c = await _db.Customers.FindAsync(GetId());
        if (c == null) return NotFound();
        c.Name = dto.Name; c.Phone = dto.Phone; c.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Password)) c.PasswordHash = _hasher.HashPassword(c, dto.Password);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("parts")]
    public IActionResult GetPartsCatalog()
    {
        var catalog = new[]
        {
            new {
                Id = 1,
                Name = "Ceramic Brake Pads",
                Price = 45.00m,
                Category = "Brakes", 
                // ✅ Frontend public folder path - images stay in frontend!
                Image = "/images/parts/pads.jpeg"
            },
            new {
                Id = 2,
                Name = "Premium Oil Filter",
                Price = 12.50m,
                Category = "Engine",
                Image = "/images/parts/Oil-filters.jpg"
            },
            new {
                Id = 3,
                Name = "Iridium Spark Plugs (4pc)",
                Price = 28.00m,
                Category = "Ignition",
                Image = "/images/parts/4pg.jpeg"
            },
            new {
                Id = 4,
                Name = "HEPA Air Filter",
                Price = 18.00m,
                Category = "Engine",
                Image = "/images/parts/Hepa.jpeg"
            },
            new {
                Id = 5,
                Name = "All-Weather Wiper Blades",
                Price = 22.00m,
                Category = "Exterior",
                Image = "/images/parts/Windshield.jpeg"
            },
            new {
                Id = 6,
                Name = "LED Headlight Bulbs",
                Price = 35.00m,
                Category = "Lighting",
                Image = "/images/parts/Led.jpeg"
            }
        };
        return Ok(catalog);
    }

    // POST: api/customers/checkout
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest(new { error = "Cart is empty" });

        var customerId = GetId();
        var total = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

        // ✅ REALISTIC PAYMENT STATUS:
        // Cash = instantly "Paid", Online methods = "Pending Verification"
        var paymentStatus = dto.PaymentMethod == "Cash" ? "Paid" : "Pending Verification";

        var transaction = new Transaction
        {
            CustomerId = customerId,
            TotalAmount = total,
            PaymentMethod = dto.PaymentMethod,
            PaymentStatus = paymentStatus, // ✅ Dynamic status
            Date = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        var items = dto.Items.Select(i => new TransactionItem
        {
            TransactionId = transaction.Id,
            PartName = i.PartName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        _db.TransactionItems.AddRange(items);
        await _db.SaveChangesAsync();

        return Ok(new { transactionId = transaction.Id, total, message = "Purchase successful!" });
    }

    [HttpGet("vehicles")]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles() =>
        Ok(await _db.Vehicles.Where(v => v.CustomerId == GetId()).OrderBy(v => v.Make).ToListAsync());

    [HttpPost("vehicles")]
    public async Task<ActionResult<Vehicle>> AddVehicle([FromBody] VehicleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _db.Vehicles.AnyAsync(v => v.LicensePlate == dto.LicensePlate))
            return BadRequest(new { error = "License plate exists" });
        var v = new Vehicle { CustomerId = GetId(), Make = dto.Make, Model = dto.Model, Year = dto.Year, LicensePlate = dto.LicensePlate, VIN = dto.VIN };
        _db.Vehicles.Add(v); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetVehicles), new { id = v.Id }, v);
    }

    [HttpPut("vehicles/{id}")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] VehicleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == GetId());
        if (v == null) return NotFound();
        v.Make = dto.Make; v.Model = dto.Model; v.Year = dto.Year; v.LicensePlate = dto.LicensePlate; v.VIN = dto.VIN;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("vehicles/{id}")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == GetId());
        if (v == null) return NotFound();
        _db.Vehicles.Remove(v); await _db.SaveChangesAsync(); return NoContent();
    }
    #endregion

    #region Feature 13: Appointments, Requests, Reviews
    [HttpPost("appointments")]
    public async Task<ActionResult<Appointment>> BookAppointment([FromBody] AppointmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Invalid input", details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        if (dto.PreferredDate <= DateTime.UtcNow)
            return BadRequest(new { error = "Preferred date must be in the future" });

        if (string.IsNullOrWhiteSpace(dto.ServiceType))
            return BadRequest(new { error = "Service type is required" });

        try
        {
            var customerId = GetId();
            var customerExists = await _db.Customers.AnyAsync(c => c.Id == customerId);
            if (!customerExists)
                return Unauthorized(new { error = "Customer not found" });

            var appointment = new Appointment
            {
                CustomerId = customerId,
                PreferredDate = dto.PreferredDate.ToUniversalTime(),
                ServiceType = dto.ServiceType?.Trim() ?? string.Empty,
                Notes = dto.Notes?.Trim(),
                Status = "Pending"
            };

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAppointments), new { id = appointment.Id }, appointment);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            System.Diagnostics.Debug.WriteLine($" Appointment save failed: {innerMsg}");

            if (innerMsg.Contains("null value in column") || innerMsg.Contains("violates not-null"))
                return BadRequest(new { error = "Missing required field. Please check your input." });
            if (innerMsg.Contains("foreign key constraint"))
                return BadRequest(new { error = "Invalid customer or vehicle reference." });
            if (innerMsg.Contains("value too long"))
                return BadRequest(new { error = "One or more fields exceed maximum length." });

            return BadRequest(new { error = "Failed to book appointment. Please try again.", details = innerMsg });
        }
    }

    [HttpGet("appointments")]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments() =>
        Ok(await _db.Appointments.Where(a => a.CustomerId == GetId()).OrderByDescending(a => a.PreferredDate).ToListAsync());

    [HttpPost("part-requests")]
    public async Task<ActionResult<PartRequest>> RequestPart([FromBody] PartRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var r = new PartRequest { CustomerId = GetId(), PartName = dto.PartName, VehicleModel = dto.VehicleModel, Description = dto.Description, Status = "Submitted" };
        _db.PartRequests.Add(r); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPartRequests), new { id = r.Id }, r);
    }

    [HttpGet("part-requests")]
    public async Task<ActionResult<IEnumerable<PartRequest>>> GetPartRequests() =>
        Ok(await _db.PartRequests.Where(r => r.CustomerId == GetId()).OrderByDescending(r => r.RequestedDate).ToListAsync());

    [HttpPost("reviews")]
    public async Task<ActionResult<Review>> SubmitReview([FromBody] ReviewDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var r = new Review { CustomerId = GetId(), Rating = dto.Rating, Comment = dto.Comment, AppointmentId = dto.AppointmentId };
        _db.Reviews.Add(r); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetReviews), new { id = r.Id }, r);
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviews() =>
        Ok(await _db.Reviews.Where(r => r.CustomerId == GetId()).OrderByDescending(r => r.CreatedAt).ToListAsync());
    #endregion

    #region Feature 14: History
    [HttpGet("history")]
    public async Task<ActionResult<object>> GetHistory()
    {
        try
        {
            var customerId = GetId();

            var purchases = await _db.Transactions
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.Date)
                .Select(t => new
                {
                    id = t.Id,
                    totalAmount = t.TotalAmount,
                    paymentMethod = t.PaymentMethod,
                    paymentStatus = t.PaymentStatus,
                    date = t.Date,
                    items = _db.TransactionItems
                        .Where(i => i.TransactionId == t.Id)
                        .Select(i => new
                        {
                            id = i.Id,
                            partName = i.PartName,
                            quantity = i.Quantity,
                            unitPrice = i.UnitPrice
                        })
                        .ToList()
                })
                .ToListAsync();

            var services = await _db.Appointments
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.PreferredDate)
                .Select(a => new
                {
                    id = a.Id,
                    preferredDate = a.PreferredDate,
                    serviceType = a.ServiceType,
                    notes = a.Notes,
                    status = a.Status
                })
                .ToListAsync();

            return Ok(new { purchases, services });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to load history",
                details = ex.Message,
                stack = ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))
            });
        }
    }
    #endregion
}
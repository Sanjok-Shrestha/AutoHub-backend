// src/AutoHub.API/Controllers/CustomersController.cs
using AutoHub.API.Data;
using AutoHub.API.Models;
using AutoHub.API.Services;
using AutoHub.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoHub.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly KhaltiService _khalti;
    private readonly DiscountService _discountService;
    private readonly InvoiceEmailService _emailService;

    public CustomersController(
        AppDbContext db,
        KhaltiService khalti,
        DiscountService discountService,
        InvoiceEmailService emailService)
    {
        _db = db;
        _khalti = khalti;
        _discountService = discountService;
        _emailService = emailService;
    }

    //  Helper: Get current customer ID from JWT claim
    private int GetId() => int.Parse(
        User.FindFirst("CustomerId")?.Value
        ?? throw new UnauthorizedAccessException("CustomerId claim missing")
    );

    //  GET: Customer Profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var customerId = GetId();
            var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == customerId);
            if (customer == null)
                return NotFound(new { error = "Customer not found" });

            var totalSpent = await _db.Sales
                .Where(t => t.CustomerId == customerId && t.PaymentStatus == "Paid")
                .SumAsync(t => (decimal?)t.TotalAmount) ?? 0m;

            return Ok(new
            {
                id = customer.Id,
                name = customer.Name ?? "",
                email = customer.Email ?? "",
                phone = customer.Phone ?? "",
                address = customer.Address ?? "",
                registeredDate = customer.RegisteredDate,
                totalSpent = totalSpent,
                emailConfirmed = customer.EmailConfirmed,
                userType = customer.UserType
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($" GetProfile error: {ex.Message}");
            return StatusCode(500, new { error = "Failed to load profile" });
        }
    }

    //  PUT: Update Customer Profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customerId = GetId();
        var customer = await _db.Customers.FindAsync(customerId);
        if (customer == null) return NotFound();

        customer.Name = dto.Name;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Address = dto.Address ?? customer.Address;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    //  GET: Parts Catalog (public)
    [HttpGet("parts")]
    [AllowAnonymous]
    public IActionResult GetPartsCatalog()
    {
        var catalog = new[]
        {
            new { Id = 1, Name = "Ceramic Brake Pads", Price = 45.00m, Category = "Brakes", Image = "/images/parts/pads.jpeg" },
            new { Id = 2, Name = "Premium Oil Filter", Price = 12.50m, Category = "Engine", Image = "/images/parts/Oil-filters.jpg" },
            new { Id = 3, Name = "Iridium Spark Plugs (4pc)", Price = 28.00m, Category = "Ignition", Image = "/images/parts/4pg.jpeg" },
            new { Id = 4, Name = "HEPA Air Filter", Price = 18.00m, Category = "Engine", Image = "/images/parts/Hepa.jpeg" },
            new { Id = 5, Name = "All-Weather Wiper Blades", Price = 22.00m, Category = "Exterior", Image = "/images/parts/Windshield.jpeg" },
            new { Id = 6, Name = "LED Headlight Bulbs", Price = 35.00m, Category = "Lighting", Image = "/images/parts/Led.jpeg" }
        };
        return Ok(catalog);
    }

    //  POST: Checkout (Cash, Khalti, or Credit) — WITH CREDIT LIMIT & FIXED NAMING
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest(new { error = "Cart is empty" });

        var customerId = GetId();
        var subtotal = dto.Items.Sum(i => i.Quantity * i.UnitPrice);
        var finalTotal = _discountService.ApplyDiscount(subtotal);
        var discountAmount = subtotal - finalTotal;
        var discountMessage = discountAmount > 0 ? "10% Loyalty Discount Applied" : "";

        //  Handle Credit Payment Method — WITH $50 LIMIT CHECK
        if (dto.PaymentMethod == "Credit")
        {
            // 1. Get customer and their credit limit
            var customer = await _db.Customers.FindAsync(customerId);
            if (customer == null) return NotFound(new { error = "Customer not found" });

            // 2. Get customer's unpaid credits
            var unpaidCredits = await _db.Credits
                .Where(c => c.CustomerId == customerId && !c.IsPaid)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();

            var totalCreditBalance = unpaidCredits.Sum(c => c.Amount);

            //  3. CHECK CREDIT LIMIT: Max $50 per transaction without admin approval
            const decimal DEFAULT_CREDIT_LIMIT = 50.00m;
            var effectiveLimit = customer.CreditLimit > 0 ? customer.CreditLimit : DEFAULT_CREDIT_LIMIT;

            if (finalTotal > effectiveLimit)
            {
                return BadRequest(new
                {
                    error = "Credit limit exceeded",
                    message = $"Credit purchases are limited to ${effectiveLimit:F2} per transaction. Please contact admin for approval or use another payment method.",
                    creditLimit = effectiveLimit,
                    requestedAmount = finalTotal,
                    availableCredit = totalCreditBalance
                });
            }

            // 4. Check if customer has enough credit balance
            if (totalCreditBalance < finalTotal)
            {
                return BadRequest(new
                {
                    error = "Insufficient credit balance",
                    availableCredit = totalCreditBalance,
                    requiredAmount = finalTotal,
                    message = $"You have ${totalCreditBalance:F2} in credit, but ${finalTotal:F2} is required."
                });
            }

            // 5. Create SALE record (fixed naming: Sale, not Transaction)
            var sale = new Sale
            {
                CustomerId = customerId,
                TotalAmount = finalTotal,
                PaymentMethod = "Credit",
                PaymentStatus = "Paid",
                Date = DateTime.UtcNow
            };

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();

            // 6. Create sale items 
            var saleItems = dto.Items.Select(i => new SaleItem
            {
                SaleId = sale.Id,
                PartName = i.PartName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            _db.SaleItems.AddRange(saleItems); 
            // 7. Deduct from credits (FIFO)
            var remainingToDeduct = finalTotal;
            foreach (var credit in unpaidCredits)
            {
                if (remainingToDeduct <= 0) break;

                var deductionAmount = Math.Min(credit.Amount, remainingToDeduct);

                if (deductionAmount >= credit.Amount)
                {
                    credit.IsPaid = true;
                    credit.SaleId = sale.Id;  
                    remainingToDeduct -= credit.Amount;
                }
                else
                {
                    var newCredit = new Credit
                    {
                        CustomerId = customerId,
                        CustomerName = credit.CustomerName,
                        CustomerEmail = credit.CustomerEmail,
                        Amount = credit.Amount - deductionAmount,
                        CreatedDate = DateTime.UtcNow,
                        IsPaid = false,
                        SaleId = null
                    };

                    credit.IsPaid = true;
                    credit.SaleId = sale.Id;
                    credit.Amount = deductionAmount;

                    _db.Credits.Add(newCredit);
                    remainingToDeduct = 0;
                }
            }

            await _db.SaveChangesAsync();

            // 8. Update customer's TotalSpent
            if (customer != null)
            {
                customer.TotalSpent += finalTotal;
                await _db.SaveChangesAsync();
            }

            // 9. Create and send invoice (same as cash flow)
            try
            {
                if (customer != null && !string.IsNullOrWhiteSpace(customer.Email))
                {
                    var invoice = new Invoice
                    {
                        CustomerName = customer.Name,
                        CustomerEmail = customer.Email,
                        TotalAmount = finalTotal,
                        FinalAmount = finalTotal,
                        IsDiscountApplied = discountAmount > 0,
                        Date = DateTime.UtcNow,
                        IsEmailSent = false,
                        Status = "Paid"
                    };

                    invoice.Items = saleItems.Select(si => new InvoiceItem
                    {
                        PartNameForDisplay = si.PartName,
                        Quantity = si.Quantity,
                        UnitPrice = si.UnitPrice,
                        Subtotal = si.Quantity * si.UnitPrice
                    }).ToList();

                    _db.Invoices.Add(invoice);
                    await _db.SaveChangesAsync();

                    var invoiceDto = new InvoiceResponseDto
                    {
                        Id = invoice.Id,
                        CustomerName = invoice.CustomerName,
                        CustomerEmail = invoice.CustomerEmail,
                        TotalAmount = invoice.TotalAmount,
                        FinalAmount = invoice.FinalAmount,
                        IsDiscountApplied = invoice.IsDiscountApplied,
                        IsEmailSent = invoice.IsEmailSent,
                        Date = invoice.Date,
                        Items = invoice.Items.Select(ii => new InvoiceItemResponseDto
                        {
                            PartName = ii.PartNameForDisplay ?? ii.Part?.Name ?? "Unknown Part",
                            Quantity = ii.Quantity,
                            UnitPrice = ii.UnitPrice,
                            Subtotal = ii.Subtotal
                        }).ToList()
                    };

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendInvoiceEmailAsync(invoiceDto);
                            invoice.IsEmailSent = true;
                            await _db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($" Invoice email failed: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Invoice creation failed: {ex.Message}");
            }

            return Ok(new
            {
                saleId = sale.Id, 
                subtotal,
                discountApplied = discountAmount,
                finalTotal,
                discountMessage,
                paymentMethod = "Credit",
                creditUsed = finalTotal,
                remainingCredit = totalCreditBalance - finalTotal,
                message = $"Purchase successful! Paid with credit (within ${effectiveLimit:F2} limit)."
            });
        }

        //  Cash Payment Flow 
        if (dto.PaymentMethod == "Cash")
        {
            var sale = new Sale
            {
                CustomerId = customerId,
                TotalAmount = finalTotal,
                PaymentMethod = "Cash",
                PaymentStatus = "Paid",
                Date = DateTime.UtcNow
            };

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();

            var saleItems = dto.Items.Select(i => new SaleItem
            {
                SaleId = sale.Id,
                PartName = i.PartName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            _db.SaleItems.AddRange(saleItems);

            var customer = await _db.Customers.FindAsync(customerId);
            if (customer != null)
            {
                customer.TotalSpent += finalTotal;
                await _db.SaveChangesAsync();
            }

            // Invoice logic (same as credit flow)
            try
            {
                if (customer != null && !string.IsNullOrWhiteSpace(customer.Email))
                {
                    var invoice = new Invoice
                    {
                        CustomerName = customer.Name,
                        CustomerEmail = customer.Email,
                        TotalAmount = finalTotal,
                        FinalAmount = finalTotal,
                        IsDiscountApplied = discountAmount > 0,
                        Date = DateTime.UtcNow,
                        IsEmailSent = false,
                        Status = "Paid"
                    };

                    invoice.Items = saleItems.Select(si => new InvoiceItem
                    {
                        PartNameForDisplay = si.PartName,
                        Quantity = si.Quantity,
                        UnitPrice = si.UnitPrice,
                        Subtotal = si.Quantity * si.UnitPrice
                    }).ToList();

                    _db.Invoices.Add(invoice);
                    await _db.SaveChangesAsync();

                    var invoiceDto = new InvoiceResponseDto
                    {
                        Id = invoice.Id,
                        CustomerName = invoice.CustomerName,
                        CustomerEmail = invoice.CustomerEmail,
                        TotalAmount = invoice.TotalAmount,
                        FinalAmount = invoice.FinalAmount,
                        IsDiscountApplied = invoice.IsDiscountApplied,
                        IsEmailSent = invoice.IsEmailSent,
                        Date = invoice.Date,
                        Items = invoice.Items.Select(ii => new InvoiceItemResponseDto
                        {
                            PartName = ii.PartNameForDisplay ?? ii.Part?.Name ?? "Unknown Part",
                            Quantity = ii.Quantity,
                            UnitPrice = ii.UnitPrice,
                            Subtotal = ii.Subtotal
                        }).ToList()
                    };

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendInvoiceEmailAsync(invoiceDto);
                            invoice.IsEmailSent = true;
                            await _db.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($" Invoice email failed: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Invoice creation failed: {ex.Message}");
            }

            return Ok(new
            {
                saleId = sale.Id,
                subtotal,
                discountApplied = discountAmount,
                finalTotal,
                discountMessage,
                message = "Purchase successful! Invoice will be sent to your email."
            });
        }

        //  Khalti Payment Flow — FIXED NAMING
        if (dto.PaymentMethod == "Khalti")
        {
            var sale = new Sale
            {
                CustomerId = customerId,
                TotalAmount = finalTotal,
                PaymentMethod = "Khalti",
                PaymentStatus = "Pending Verification",
                Date = DateTime.UtcNow
            };

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();

            var saleItems = dto.Items.Select(i => new SaleItem
            {
                SaleId = sale.Id,
                PartName = i.PartName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            _db.SaleItems.AddRange(saleItems);

            var customer = await _db.Customers.FindAsync(customerId);
            var khaltiResponse = await _khalti.InitiatePaymentAsync(
                finalTotal,
                sale.Id.ToString(),
                customer?.Email ?? "customer@autohub.com"
            );

            return Ok(new
            {
                saleId = sale.Id,
                subtotal,
                discountApplied = discountAmount,
                finalTotal,
                discountMessage,
                paymentUrl = khaltiResponse.payment_url,
                pidx = khaltiResponse.pidx
            });
        }

        return BadRequest(new { error = "Unsupported payment method. Use 'Cash', 'Khalti', or 'Credit'." });
    }

    //  GET: Customer Vehicles
    [HttpGet("vehicles")]
    public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
    {
        var customerId = GetId();
        return Ok(await _db.Vehicles
            .Where(v => v.CustomerId == customerId)
            .OrderBy(v => v.Make)
            .ToListAsync());
    }

    //  POST: Add Vehicle
    [HttpPost("vehicles")]
    public async Task<ActionResult<Vehicle>> AddVehicle([FromBody] VehicleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customerId = GetId();

        if (await _db.Vehicles.AnyAsync(v => v.LicensePlate == dto.LicensePlate))
            return BadRequest(new { error = "License plate already exists" });

        var vehicle = new Vehicle
        {
            CustomerId = customerId,
            Make = dto.Make,
            Model = dto.Model,
            Year = dto.Year,
            LicensePlate = dto.LicensePlate,
            VIN = dto.VIN
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVehicles), new { id = vehicle.Id }, vehicle);
    }

    //  PUT: Update Vehicle
    [HttpPut("vehicles/{id}")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] VehicleDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customerId = GetId();
        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.CustomerId == customerId);

        if (vehicle == null) return NotFound();

        vehicle.Make = dto.Make;
        vehicle.Model = dto.Model;
        vehicle.Year = dto.Year;
        vehicle.LicensePlate = dto.LicensePlate;
        vehicle.VIN = dto.VIN;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    //  DELETE: Remove Vehicle
    [HttpDelete("vehicles/{id}")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var customerId = GetId();
        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id && v.CustomerId == customerId);

        if (vehicle == null) return NotFound();

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    //  POST: Book Appointment
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

    //  GET: Customer Appointments
    [HttpGet("appointments")]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
    {
        var customerId = GetId();
        return Ok(await _db.Appointments
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.PreferredDate)
            .ToListAsync());
    }

    //  POST: Request Part
    [HttpPost("part-requests")]
    public async Task<ActionResult<PartRequest>> RequestPart([FromBody] PartRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customerId = GetId();
        var request = new PartRequest
        {
            CustomerId = customerId,
            PartName = dto.PartName,
            VehicleModel = dto.VehicleModel,
            Description = dto.Description,
            Status = "Submitted"
        };

        _db.PartRequests.Add(request);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPartRequests), new { id = request.Id }, request);
    }

    //  GET: Customer Part Requests
    [HttpGet("part-requests")]
    public async Task<ActionResult<IEnumerable<PartRequest>>> GetPartRequests()
    {
        var customerId = GetId();
        return Ok(await _db.PartRequests
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.RequestedDate)
            .ToListAsync());
    }

    //  POST: Submit Review
    [HttpPost("reviews")]
    public async Task<ActionResult<Review>> SubmitReview([FromBody] ReviewDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var customerId = GetId();
        var review = new Review
        {
            CustomerId = customerId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            AppointmentId = dto.AppointmentId
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReviews), new { id = review.Id }, review);
    }

    //  GET: Customer Reviews
    [HttpGet("reviews")]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
    {
        var customerId = GetId();
        return Ok(await _db.Reviews
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync());
    }

    //  GET: Customer History
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        try
        {
            var customerId = GetId();

            // Get purchase history from Sales table
            var purchases = await _db.Sales  //
                .Where(s => s.CustomerId == customerId)
                .OrderByDescending(s => s.Date)
                .Select(s => new
                {
                    id = s.Id,
                    totalAmount = s.TotalAmount,
                    paymentMethod = s.PaymentMethod,
                    paymentStatus = s.PaymentStatus,
                    date = s.Date,
                    items = _db.SaleItems  
                        .Where(i => i.SaleId == s.Id)  
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
            return StatusCode(500, new { error = "Failed to load history", details = ex.Message });
        }
    }
}
// AutoHub.API/Services/CustomerService.cs
using AutoHub.API.Data;
using AutoHub.API.Models;
using AutoHub.API.Services;
using AutoHub.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Customer> _hasher = new();
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(AppDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CustomerResponseDto> CreateCustomerAsync(CustomerCreateDto dto)
    {
        try
        {
            if (await _context.Customers.AnyAsync(c => c.Phone == dto.Phone))
                throw new InvalidOperationException("A customer with this phone number already exists.");

           
            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                await _context.Customers.AnyAsync(c =>
                    !string.IsNullOrWhiteSpace(c.Email) &&
                    c.Email.ToLower() == dto.Email.ToLower()))
                throw new InvalidOperationException("A customer with this email already exists.");

           
            var passwordHash = _hasher.HashPassword(new Customer(), dto.Password);

         
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Vehicle.Year, @"^\d{4}$") ||
                int.Parse(dto.Vehicle.Year) < 1900 || int.Parse(dto.Vehicle.Year) > 2030)
            {
                throw new ArgumentException("Vehicle year must be a 4-digit number between 1900 and 2030");
            }

            var customer = new Customer
            {
                Name = dto.Name?.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim().ToLower(),  
                Address = dto.Address?.Trim(),
                RegisteredDate = DateTime.UtcNow,
                PasswordHash = passwordHash,
                EmailConfirmed = true,
                IsActive = true,
                Role = "Customer",
                UserType = "Customer",
                Vehicles = new List<Vehicle>
                {
                    new()
                    {
                        Make = dto.Vehicle.Make?.Trim(),
                        Model = dto.Vehicle.Model?.Trim(),
                        Year = dto.Vehicle.Year,  // ✅ Entity gets STRING (no conversion!)
                        LicensePlate = dto.Vehicle.LicensePlate?.Trim().ToUpper(),
                        VIN = dto.Vehicle.VIN?.Trim()?.Substring(0, Math.Min(17, dto.Vehicle.VIN?.Length ?? 0))
                    }
                }
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Customer created: {Email} (Id: {Id})", customer.Email, customer.Id);

            return await GetCustomerByIdAsync(customer.Id);
        }
        catch (DbUpdateException dbEx)
        {
            
            _logger.LogError(dbEx, " Database error: {Message}", dbEx.Message);
            _logger.LogError(" Inner exception: {Inner}", dbEx.InnerException?.Message);

            // Return user-friendly error for constraint violations
            if (dbEx.InnerException?.Message?.Contains("unique constraint") == true ||
                dbEx.InnerException?.Message?.Contains("duplicate key") == true)
            {
                throw new InvalidOperationException("A customer or vehicle with these details already exists. Please check phone, email, or license plate.");
            }
            throw; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Unexpected error: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<CustomerResponseDto> GetCustomerByIdAsync(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Vehicles)
                .ThenInclude(v => v.ServiceHistories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
            throw new KeyNotFoundException($"Customer with ID {id} not found");

        return MapToResponseDto(customer);
    }

    public async Task<List<CustomerResponseDto>> SearchCustomersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<CustomerResponseDto>();

        var lowerQuery = query.ToLower().Trim();

        var customers = await _context.Customers
            .Include(c => c.Vehicles)
                .ThenInclude(v => v.ServiceHistories)
            .Where(c =>
                c.Name.ToLower().Contains(lowerQuery) ||
                (c.Email != null && c.Email.ToLower().Contains(lowerQuery)) ||
                c.Phone.Contains(lowerQuery) ||
                c.Vehicles.Any(v =>
                    v.Make.ToLower().Contains(lowerQuery) ||
                    v.Model.ToLower().Contains(lowerQuery) ||
                    v.LicensePlate.ToLower().Contains(lowerQuery)))
            .ToListAsync();

        return customers.Select(MapToResponseDto).ToList();
    }


    private CustomerResponseDto MapToResponseDto(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        RegisteredDate = c.RegisteredDate,
        Vehicles = c.Vehicles.Select(v => new VehicleDto
        {
            Id = v.Id,
            Make = v.Make,
            Model = v.Model,
            Year = v.Year,  
            LicensePlate = v.LicensePlate,
            VIN = v.VIN,
            ServiceHistories = v.ServiceHistories
                .OrderBy(sh => sh.ServiceDate)
                .Select(sh => new ServiceHistoryDto
                {
                    Id = sh.Id,
                    ServiceDate = sh.ServiceDate,
                    Description = sh.Description,
                    Cost = sh.Cost,
                    Status = sh.Status
                }).ToList()
        }).ToList()
    };
}
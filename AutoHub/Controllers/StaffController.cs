using AutoHub.API.Data;
using AutoHub.API.Models;
using AutoHub.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoHub.API.Controllers
{
   
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<Customer> _hasher = new();
        private readonly ILogger<StaffController> _logger;

        public StaffController(AppDbContext context, ILogger<StaffController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/staff
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Staff>>> GetStaff()
        {
            return await _context.Staffs.OrderBy(s => s.Name).ToListAsync();
        }

        // GET: api/staff/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Staff>> GetStaffById(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();
            return staff;
        }

        // POST: api/staff - Create staff + auto-generate login account
      
        [HttpPost]
        public async Task<ActionResult<Staff>> CreateStaff(StaffDto dto)
        {
            // 1️⃣ Validate input
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { error = "Name is required" });
            if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains("@"))
                return BadRequest(new { error = "Valid email is required" });
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
                return BadRequest(new { error = "Password must be at least 8 characters" });

            // 2️⃣ Check email uniqueness
            var emailLower = dto.Email.ToLower().Trim();
            if (await _context.Staffs.AnyAsync(s => s.Email == emailLower) ||
                await _context.Customers.AnyAsync(c => c.Email == emailLower))
                return BadRequest(new { error = "Email already registered in the system" });

            // 3️⃣ Create Auth Account (Customers table) → Enables /api/auth/login
            var authAccount = new Customer
            {
                Name = dto.Name.Trim(),
                Email = emailLower,
                Phone = dto.Phone?.Trim() ?? string.Empty,
                Address = "", // Staff don't need customer address
                PasswordHash = _hasher.HashPassword(new Customer(), dto.Password),
                EmailConfirmed = true,
                Role = dto.Role ?? "Staff",
                UserType = "Staff",
                RegisteredDate = DateTime.UtcNow,
                IsActive = true,
                TotalSpent = 0
            };
            _context.Customers.Add(authAccount);
            await _context.SaveChangesAsync();

            // 4️⃣ Create Operational Record (Staffs table)
            var staff = new Staff
            {
                Name = dto.Name.Trim(),
                Email = emailLower,
                Phone = dto.Phone?.Trim() ?? string.Empty,
                Role = dto.Role ?? "Staff",
                Status = dto.Status ?? "Active",
                Photo = dto.Photo
            };
            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Staff created: {Name} (Email: {Email}, Auth ID: {AuthId})",
                staff.Name, staff.Email, authAccount.Id);

            return CreatedAtAction(nameof(GetStaffById), new { id = staff.Id }, staff);
        }

        // PUT: api/staff/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(int id, StaffDto dto)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();

            var emailLower = dto.Email?.ToLower().Trim();
            if (!string.IsNullOrEmpty(emailLower) &&
                await _context.Staffs.AnyAsync(s => s.Email == emailLower && s.Id != id))
                return BadRequest(new { error = "Email already in use by another staff member" });

            // Update operational record
            staff.Name = dto.Name?.Trim() ?? staff.Name;
            staff.Email = emailLower ?? staff.Email;
            staff.Phone = dto.Phone?.Trim() ?? staff.Phone;
            staff.Role = dto.Role ?? staff.Role;
            staff.Status = dto.Status ?? staff.Status;
            staff.Photo = dto.Photo ?? staff.Photo;

            //  Sync auth account if it exists (email/role changes)
            var authAccount = await _context.Customers.FirstOrDefaultAsync(c => c.Email == staff.Email);
            if (authAccount != null)
            {
                authAccount.Name = staff.Name;
                authAccount.Phone = staff.Phone;
                authAccount.Role = staff.Role;
                // Note: Password/Email changes require dedicated endpoints for security
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Staff updated: {Name} (ID: {Id})", staff.Name, id);

            return NoContent();
        }

        // DELETE: api/staff/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();

            // Remove linked auth account (so staff can't login after deletion)
            var authAccount = await _context.Customers.FirstOrDefaultAsync(c => c.Email == staff.Email && c.UserType == "Staff");
            if (authAccount != null)
            {
                _context.Customers.Remove(authAccount);
            }

            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Staff deleted: {Name} (ID: {Id})", staff.Name, id);

            return NoContent();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementSystem.VehiclePartsAPI.Data;
using VehicleManagementSystem.VehiclePartsAPI.DTOs;
using VehicleManagementSystem.VehiclePartsAPI.Models;

namespace VehicleManagementSystem.VehiclePartsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/staff
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Staff>>> GetStaff()
        {
            return await _context.Staffs.ToListAsync();
        }

        // GET: api/staff/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Staff>> GetStaffById(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);

            if (staff == null)
                return NotFound();

            return staff;
        }

        // POST: api/staff
        [HttpPost]
        public async Task<ActionResult<Staff>> CreateStaff(StaffDto dto)
        {
            var staff = new Staff
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Role = dto.Role,
                Status = dto.Status,
                Photo = dto.Photo
            };

            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStaffById), new { id = staff.Id }, staff);
        }

        // PUT: api/staff/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(int id, StaffDto dto)
        {
            var staff = await _context.Staffs.FindAsync(id);

            if (staff == null)
                return NotFound();

            staff.Name = dto.Name;
            staff.Email = dto.Email;
            staff.Phone = dto.Phone;
            staff.Role = dto.Role;
            staff.Status = dto.Status;
            staff.Photo = dto.Photo;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/staff/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);

            if (staff == null)
                return NotFound();

            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
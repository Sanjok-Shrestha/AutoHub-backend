using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoHub.API.Data;
using AutoHub.API.Models;
using AutoHub.DTOs;

namespace AutoHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VendorsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/vendors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vendor>>> GetVendors()
        {
            return await _context.Vendors.ToListAsync();
        }

        // GET: api/vendors/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Vendor>> GetVendor(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
                return NotFound();

            return vendor;
        }

        // POST: api/vendors
        [HttpPost]
        public async Task<ActionResult<Vendor>> CreateVendor(VendorDto dto)
        {
            var vendor = new Vendor
            {
                VendorName = dto.VendorName,
                ContactPerson = dto.ContactPerson,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                Status = dto.Status
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVendor), new { id = vendor.Id }, vendor);
        }

        // PUT: api/vendors/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVendor(int id, VendorDto dto)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
                return NotFound();

            vendor.VendorName = dto.VendorName;
            vendor.ContactPerson = dto.ContactPerson;
            vendor.Phone = dto.Phone;
            vendor.Email = dto.Email;
            vendor.Address = dto.Address;
            vendor.Status = dto.Status;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/vendors/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
                return NotFound();

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoHub.API.Data;
using AutoHub.API.Models;
    
[ApiController]
[Route("api/[controller]")]
public class PartsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PartsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _context.Parts.ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Add(Part part)
    {
        _context.Parts.Add(part);
        await _context.SaveChangesAsync();
        return Ok(part);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Part updatedPart)
    {
        var part = await _context.Parts.FindAsync(id);

        if (part == null)
            return NotFound();

        part.Name = updatedPart.Name;
        part.Quantity = updatedPart.Quantity;
        part.Price = updatedPart.Price;

        await _context.SaveChangesAsync();

        return Ok(part);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var part = await _context.Parts.FindAsync(id);
        if (part == null) return NotFound();

        _context.Parts.Remove(part);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
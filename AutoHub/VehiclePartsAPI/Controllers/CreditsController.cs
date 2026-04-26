using Microsoft.AspNetCore.Mvc;
using VehicleManagementSystem.VehiclePartsAPI.Data;
using VehicleManagementSystem.VehiclePartsAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class CreditsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CreditsController(AppDbContext context)
    {
        _context = context;
    }

    //ADD CREDIT (for testing)
    [HttpPost]
    public async Task<IActionResult> AddCredit(Credit credit)
    {
        _context.Credits.Add(credit);
        await _context.SaveChangesAsync();

        return Ok(credit);
    }

    // (optional) view all credits
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_context.Credits.ToList());
    }
}
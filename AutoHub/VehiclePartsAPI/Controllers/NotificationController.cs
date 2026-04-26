using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementSystem.VehiclePartsAPI.Data;
using VehicleManagementSystem.VehiclePartsAPI.Models;
using VehicleManagementSystem.VehiclePartsAPI.Services;

namespace VehiclePartsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _service;
        private readonly AppDbContext _context;

        public NotificationController(NotificationService service)
        {
            _service = service;
        }

        [HttpGet("low-stock")]
        public IActionResult GetLowStock()
        {
            return Ok(_service.GetLowStockParts());
        }

        [HttpGet("has-low-stock")]
        public IActionResult HasLowStock()
        {
            return Ok(_service.HasLowStock());
        }

        [HttpGet("overdue")]
        public IActionResult GetOverdue()
        {
            return Ok(_service.GetOverdueCredits());
        }

    }
}
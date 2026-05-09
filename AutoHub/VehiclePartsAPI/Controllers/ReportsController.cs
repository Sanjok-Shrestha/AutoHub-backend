using Microsoft.AspNetCore.Mvc;
using VehicleManagementSystem.VehiclePartsAPI.Services;

namespace VehicleManagementSystem.VehiclePartsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportsController(ReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: api/reports/top-spenders
        [HttpGet("top-spenders")]
        public async Task<IActionResult> GetTopSpenders()
        {
            var result = await _reportService.GetTopSpendersAsync();
            return Ok(result);
        }

        // GET: api/reports/regular-customers
        [HttpGet("regular-customers")]
        public async Task<IActionResult> GetRegularCustomers()
        {
            var result = await _reportService.GetRegularCustomersAsync();
            return Ok(result);
        }

        // GET: api/reports/overdue-credits
        [HttpGet("overdue-credits")]
        public async Task<IActionResult> GetOverdueCredits()
        {
            var result = await _reportService.GetOverdueCreditsAsync();
            return Ok(result);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using VehicleManagementSystem.VehiclePartsAPI.DTOs;
using VehicleManagementSystem.VehiclePartsAPI.Services;

namespace VehicleManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly DiscountService _discountService;

        public SalesController(DiscountService discountService)
        {
            _discountService = discountService;
        }

        [HttpPost]
        public IActionResult CreateSale([FromBody] SaleRequestDto request)
        {
            if (request == null)
                return BadRequest();

            var original = request.Amount;

            var final = _discountService.ApplyDiscount(original);

            return Ok(new
            {
                original = original,
                final = final
            });
        }
    }
}
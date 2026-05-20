// AutoHub.API/Controllers/StaffCustomersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoHub.API.Services;
using AutoHub.DTOs;

namespace AutoHub.API.Controllers;

[ApiController]
[Route("api/staff/customers")]

public class StaffCustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public StaffCustomersController([FromServices] ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>
    /// Register a new customer with their first vehicle
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "Validation failed", details = ModelState });

        try
        {
            //  Service layer handles password hashing internally
            var created = await _customerService.CreateCustomerAsync(dto);
            return CreatedAtAction(nameof(GetCustomerById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("email") || ex.Message.Contains("phone"))
        {
            return Conflict(new { error = "Duplicate entry", message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($" CreateCustomer failed: {ex.Message}");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get customer details by ID including vehicles
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            return Ok(customer);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = "Not found", message = ex.Message });
        }
    }

    /// <summary>
    /// Search customers by name, email, phone, or vehicle plate
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchCustomers([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(new List<CustomerResponseDto>());

        var results = await _customerService.SearchCustomersAsync(query);
        return Ok(results);
    }
}
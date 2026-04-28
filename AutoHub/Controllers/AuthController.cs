using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims; // Required for JWT, but simplified to basic strings
using System.Text;
using AutoHub.API.Data;
using AutoHub.API.Models;
using AutoHub.API.Services;

namespace AutoHub.API.Controllers;

[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;
    private readonly PasswordHasher<Customer> _hasher = new();
    private readonly ILogger<AuthController> _log;

    public AuthController(ApplicationDbContext db, IConfiguration config, IEmailService email, ILogger<AuthController> log)
    {
        _db = db;
        _config = config;
        _email = email;
        _log = log;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _db.Customers.AnyAsync(c => c.Email == dto.Email))
            return BadRequest(new { error = "Email already exists" });

        // ✅ Simple token generation using Guid (no cryptography needed)
        var token = Guid.NewGuid().ToString("N");

        var cust = new Customer
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            PasswordHash = _hasher.HashPassword(new Customer(), dto.Password),
            EmailConfirmationToken = token,
            TokenExpiry = DateTime.UtcNow.AddHours(24),
            Role = "Customer",
            RegisteredDate = DateTime.UtcNow
        };

        _db.Customers.Add(cust);
        await _db.SaveChangesAsync();

        // Fire & forget email sending
        _ = Task.Run(async () => await _email.SendConfirmationEmailAsync(cust.Email, cust.Name, token));

        return Ok(new { message = "Registration successful. Check your email to confirm.", requiresConfirmation = true });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest(new { error = "Invalid token" });

        var cust = await _db.Customers.FirstOrDefaultAsync(c =>
            c.EmailConfirmationToken == token && c.TokenExpiry > DateTime.UtcNow && !c.EmailConfirmed);

        if (cust == null) return BadRequest(new { error = "Invalid or expired token" });

        cust.EmailConfirmed = true;
        cust.EmailConfirmationToken = null;
        cust.TokenExpiry = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Email confirmed! You can now login.", email = cust.Email });
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> Resend([FromBody] ResendConfirmationDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var cust = await _db.Customers.FirstOrDefaultAsync(c => c.Email == dto.Email && !c.EmailConfirmed);
        if (cust == null) return Ok(new { message = "If email exists, confirmation sent." });

        var token = Guid.NewGuid().ToString("N");
        cust.EmailConfirmationToken = token;
        cust.TokenExpiry = DateTime.UtcNow.AddHours(24);
        await _db.SaveChangesAsync();

        var ok = await _email.SendConfirmationEmailAsync(cust.Email, cust.Name, token);
        return ok ? Ok(new { message = "Confirmation email resent." }) : StatusCode(500, new { error = "Failed to send." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _db.Customers.FirstOrDefaultAsync(c => c.Email == dto.Email);
        if (user == null) return Unauthorized(new { error = "Invalid credentials" });
        if (!user.EmailConfirmed) return Unauthorized(new { error = "Please confirm your email first.", requiresConfirmation = true, email = user.Email });

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) != PasswordVerificationResult.Success)
            return Unauthorized(new { error = "Invalid credentials" });

        return Ok(new AuthResponse { Token = GenJwt(user), CustomerId = user.Id, Name = user.Name, Role = user.Role });
    }

    // ✅ Simplified JWT generation using plain strings (no advanced claim types)
    private string GenJwt(Customer u)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", u.Id.ToString()),
            new Claim("CustomerId", u.Id.ToString()),
            new Claim("role", u.Role),
            new Claim("email", u.Email),
            new Claim("jti", Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"],
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
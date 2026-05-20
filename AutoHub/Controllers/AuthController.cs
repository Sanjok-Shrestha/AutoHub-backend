// AutoHub.API/Controllers/AuthController.cs
using AutoHub.API.Data;
using AutoHub.API.Models;
using AutoHub.API.Services;
using AutoHub.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AutoHub.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;
    private readonly PasswordHasher<Customer> _hasher = new();
    private readonly ILogger<AuthController> _log;

    private static readonly ConcurrentDictionary<string, (int count, DateTime reset)> _loginAttempts = new();

    public AuthController(
        AppDbContext db,
        IConfiguration config,
        IEmailService email,
        ILogger<AuthController> log)
    {
        _db = db;
        _config = config;
        _email = email;
        _log = log;
    }

    private bool IsRateLimited(string ip)
    {
        var now = DateTime.UtcNow;
        if (!_loginAttempts.TryGetValue(ip, out var attempt))
        {
            _loginAttempts[ip] = (1, now.AddMinutes(5));
            return false;
        }
        if (now > attempt.reset)
        {
            _loginAttempts[ip] = (1, now.AddMinutes(5));
            return false;
        }
        if (attempt.count >= 5) return true;
        _loginAttempts[ip] = (attempt.count + 1, attempt.reset);
        return false;
    }

    /// <summary>
    /// Register NEW CUSTOMER (self-service with email confirmation)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            _log.LogWarning("Registration failed: invalid model state for {Email}", dto.Email);
            return BadRequest(ModelState);
        }

     
        if (await _db.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower()))
        {
            _log.LogWarning("Registration blocked: email already exists {Email}", dto.Email);
            return BadRequest(new { error = "Email already registered" });
        }

        var token = Guid.NewGuid().ToString("N");

        var cust = new Customer
        {
            Name = dto.Name,
            Email = dto.Email.ToLower().Trim(),  //  Always store lowercase
            Phone = dto.Phone,
            Address = dto.Address,
            PasswordHash = _hasher.HashPassword(new Customer(), dto.Password),
            EmailConfirmationToken = token,
            TokenExpiry = DateTime.UtcNow.AddHours(24),
            EmailConfirmed = false,
            Role = "Customer",
            UserType = "Customer",
            RegisteredDate = DateTime.UtcNow,
            IsActive = true
        };

        _db.Customers.Add(cust);
        await _db.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                await _email.SendConfirmationEmailAsync(cust.Email, cust.Name, token);
                _log.LogInformation("Confirmation email sent to {Email}", cust.Email);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to send confirmation email to {Email}", cust.Email);
            }
        });

        _log.LogInformation("New customer registered: {Email}", dto.Email);

        return Ok(new
        {
            message = "Registration successful. Check your email to confirm your account.",
            requiresConfirmation = true
        });
    }

    /// <summary>
    /// Confirm email using token from registration email
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _log.LogWarning("Email confirmation failed: missing token");
            return BadRequest(new { error = "Invalid token" });
        }

        var cust = await _db.Customers.FirstOrDefaultAsync(c =>
            c.EmailConfirmationToken == token &&
            c.TokenExpiry > DateTime.UtcNow &&
            !c.EmailConfirmed);

        if (cust == null)
        {
            _log.LogWarning("Email confirmation failed: invalid or expired token");
            return BadRequest(new { error = "Invalid or expired confirmation link" });
        }

        cust.EmailConfirmed = true;
        cust.EmailConfirmationToken = null;
        cust.TokenExpiry = null;
        await _db.SaveChangesAsync();

        _log.LogInformation("Email confirmed for customer {Email}", cust.Email);

        return Ok(new
        {
            message = "Email confirmed! You can now log in.",
            email = cust.Email
        });
    }

    /// <summary>
    /// Resend confirmation email for unconfirmed accounts
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> Resend([FromBody] ResendConfirmationDto dto)
    {
        if (!ModelState.IsValid)
        {
            _log.LogWarning("Resend confirmation failed: invalid model state for {Email}", dto.Email);
            return BadRequest(ModelState);
        }

        //  FIXED: Case-insensitive email check
        var cust = await _db.Customers.FirstOrDefaultAsync(c =>
            c.Email.ToLower() == dto.Email.ToLower() && !c.EmailConfirmed);

        if (cust == null)
        {
            _log.LogInformation("Resend confirmation requested for unknown/unconfirmed email {Email}", dto.Email);
            return Ok(new { message = "If this email is registered and unconfirmed, a new confirmation link has been sent." });
        }

        var token = Guid.NewGuid().ToString("N");
        cust.EmailConfirmationToken = token;
        cust.TokenExpiry = DateTime.UtcNow.AddHours(24);
        await _db.SaveChangesAsync();

        try
        {
            await _email.SendConfirmationEmailAsync(cust.Email, cust.Name, token);
            _log.LogInformation("Confirmation email resent to {Email}", cust.Email);
            return Ok(new { message = "Confirmation email resent. Please check your inbox." });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to resend confirmation email to {Email}", cust.Email);
            return StatusCode(500, new { error = "Failed to send confirmation email. Please try again later." });
        }
    }

    /// <summary>
    /// Login endpoint for ALL roles: Customer, Admin, Staff
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            _log.LogWarning("Login failed: invalid model state for {Email}", dto.Email);
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (IsRateLimited(ip))
        {
            _log.LogWarning("Login blocked: rate limit exceeded for IP {IP}", ip);
            return StatusCode(429, new { error = "Too many login attempts. Please try again in 5 minutes." });
        }

        _log.LogInformation(" Login attempt for: {Email}", dto.Email);

        //  FIXED: Case-insensitive email lookup
        var user = await _db.Customers.FirstOrDefaultAsync(c =>
            c.Email.ToLower() == dto.Email.ToLower().Trim());

        if (user == null)
        {
            _log.LogWarning("Login failed: user not found {Email}", dto.Email);
            return Unauthorized(new { error = "Invalid credentials" });
        }

        if (!user.EmailConfirmed)
        {
            _log.LogWarning("Login blocked: unconfirmed email {Email}", dto.Email);
            return Unauthorized(new
            {
                error = "Please confirm your email before logging in.",
                requiresConfirmation = true,
                email = user.Email
            });
        }

        // ✅ Verify password using ASP.NET Core Identity PasswordHasher
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result != PasswordVerificationResult.Success)
        {
            _log.LogWarning("Login failed: invalid password for {Email} (Result: {Result})", dto.Email, result);
            return Unauthorized(new { error = "Invalid credentials" });
        }

        // ✅ Generate JWT token with UserType claim
        var token = GenJwt(user);

        _log.LogInformation(" User {Email} logged in successfully as {Role} (UserType: {UserType})",
            dto.Email, user.Role, user.UserType);

        return Ok(new AuthResponse
        {
            Token = token,
            CustomerId = user.Id,
            Name = user.Name,
            Role = user.Role,
            Email = user.Email,
            UserType = user.UserType,  //  Critical for frontend routing
            ExpiresIn = 28800  // 8 hours in seconds
        });
    }

    /// <summary>
    /// Get current authenticated user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
        {
            _log.LogWarning("GetCurrentUser failed: invalid or missing CustomerId claim");
            return Unauthorized(new { error = "Invalid token" });
        }

        var user = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId);

        if (user == null)
        {
            _log.LogWarning("GetCurrentUser failed: user not found for id {CustomerId}", customerId);
            return NotFound(new { error = "User not found" });
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Name,
            user.Phone,
            user.Address,
            user.Role,
            user.UserType,  //  For frontend UI decisions
            user.EmailConfirmed,
            user.RegisteredDate
        });
    }

    /// <summary>
    /// Refresh JWT token (placeholder)
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public IActionResult RefreshToken([FromBody] RefreshTokenDto dto)
    {
        _log.LogWarning("Refresh token endpoint called but not fully implemented");
        return Ok(new
        {
            token = "new_jwt_token_placeholder",
            message = "Refresh token logic to be implemented"
        });
    }

    /// <summary>
    /// Logout endpoint
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        var email = User.FindFirst("email")?.Value ?? "unknown";
        _log.LogInformation("User {Email} logged out", email);
        return Ok(new { message = "Logged out successfully" });
    }

    //  JWT Generation Helper — ADD UserType CLAIM
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
            new Claim("name", u.Name),
            new Claim("UserType", u.UserType),  //  Critical for frontend routing
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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
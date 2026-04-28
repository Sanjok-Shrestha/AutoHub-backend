using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AutoHub.API.Data;
using AutoHub.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

// ✅ Services - Conditional registration based on environment
if (builder.Environment.IsDevelopment())
{
    // 🧪 Use mock email service in development (logs to console, no SMTP)
    builder.Services.AddScoped<IEmailService, MockEmailService>();
}
else
{
    // 📧 Use real SMTP email service in production
    builder.Services.AddScoped<IEmailService, EmailService>();
}

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!))
    };
});

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("Customer", p => p.RequireRole("Customer"));
    opt.AddPolicy("Staff", p => p.RequireRole("Staff", "Admin"));
    opt.AddPolicy("Admin", p => p.RequireRole("Admin"));
});

// CORS
builder.Services.AddCors(opt => opt.AddPolicy("ReactApp", p =>
    p.WithOrigins(
        "http://localhost:5173",
        "https://localhost:5173"
    )
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .SetPreflightMaxAge(TimeSpan.FromHours(1))
));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Development middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // ❌ Disabled in dev to avoid CORS redirect issues
    // app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

// ✅ Critical order: Cors → Auth → Authorization → Controllers
app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
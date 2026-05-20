// Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;  //  Required for PasswordHasher<Customer>
using System.Text;
using AutoHub.API.Data;
using AutoHub.API.Services;
using AutoHub.API.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
    ));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),

            //  CRITICAL FIX: Tell ASP.NET Core which claim contains the role
            RoleClaimType = "role",    // ← Fixes 403 Forbidden
            NameClaimType = "name"
        };

        //  Optional: Log JWT events for debugging
        opt.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, " JWT Authentication FAILED");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug(" JWT Token validated for user {Email}",
                    context.Principal?.FindFirst("email")?.Value ?? "unknown");
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("Customer", p => p.RequireRole("Customer"));
    opt.AddPolicy("Staff", p => p.RequireRole("Staff", "Admin"));
    opt.AddPolicy("Admin", p => p.RequireRole("Admin"));
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});


if (builder.Environment.IsDevelopment())

    builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<KhaltiService>();
builder.Services.AddScoped<InvoiceEmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            var migrationLogger = services.GetRequiredService<ILogger<Program>>(); // ✅ Unique name
            migrationLogger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pendingMigrations.Count(),
                string.Join(", ", pendingMigrations));

            await db.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var migrationLogger = services.GetRequiredService<ILogger<Program>>(); // ✅ Unique name
        migrationLogger.LogError(ex, "An error occurred while migrating the database.");
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    var hasher = new PasswordHasher<Customer>();

    //  FIXED: Declare seedLogger ONCE at the top of the block
    var seedLogger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        if (!await db.Customers.AnyAsync(c => c.UserType == "Admin"))
        {
            seedLogger.LogInformation(" Creating default admin account...");

            var adminEmail = builder.Configuration["AdminSeed:Email"] ?? "admin@autohub.com";
            var adminPassword = builder.Configuration["AdminSeed:Password"] ?? "AdminPass123!";

            var admin = new Customer
            {
                Name = "System Administrator",
                Email = adminEmail,
                Phone = "9800000000",
                Address = "AutoHub HQ, Nepal",
                PasswordHash = hasher.HashPassword(new Customer(), adminPassword),
                EmailConfirmed = true,
                Role = "Admin",
                UserType = "Admin",
                RegisteredDate = DateTime.UtcNow,
                IsActive = true,
                TotalSpent = 0
            };

            db.Customers.Add(admin);
            await db.SaveChangesAsync();

            seedLogger.LogInformation(" Default admin created: {Email}", adminEmail);
            seedLogger.LogWarning("  PRODUCTION WARNING: Change the default password immediately!");
            seedLogger.LogInformation(" Default credentials: {Email} / {Password}", adminEmail, adminPassword);
        }
        else
        {
            seedLogger.LogInformation(" Admin account(s) already exist - skipping seed");
        }
    }
    catch (Exception ex)
    {
        //  FIXED: Reuse seedLogger (don't redeclare with 'var')
        seedLogger.LogError(ex, " Failed to seed admin account");
    }
}

app.Run();
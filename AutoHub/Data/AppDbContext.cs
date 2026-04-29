using Microsoft.EntityFrameworkCore;
using AutoHub.API.Models;

namespace AutoHub.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<PartRequest> PartRequests { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionItem> TransactionItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL lowercase convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            entity.SetTableName(entity.GetTableName()?.ToLower());

        // Unique indexes
        modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(v => v.LicensePlate).IsUnique();

        // Decimal precision for monetary values
        modelBuilder.Entity<Customer>().Property(c => c.TotalSpent).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<Transaction>().Property(t => t.TotalAmount).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<TransactionItem>().Property(ti => ti.UnitPrice).HasColumnType("numeric(18,2)");

        // Relationships
        modelBuilder.Entity<Vehicle>().HasOne(v => v.Customer).WithMany(c => c.Vehicles).HasForeignKey(v => v.CustomerId);
        modelBuilder.Entity<Appointment>().HasOne(a => a.Customer).WithMany(c => c.Appointments).HasForeignKey(a => a.CustomerId);
        modelBuilder.Entity<PartRequest>().HasOne(p => p.Customer).WithMany(c => c.PartRequests).HasForeignKey(p => p.CustomerId);
        modelBuilder.Entity<Review>().HasOne(r => r.Customer).WithMany(c => c.Reviews).HasForeignKey(r => r.CustomerId);
        modelBuilder.Entity<Transaction>().HasOne(t => t.Customer).WithMany(c => c.Transactions).HasForeignKey(t => t.CustomerId);
        modelBuilder.Entity<TransactionItem>().HasOne(ti => ti.Transaction).WithMany(t => t.Items).HasForeignKey(ti => ti.TransactionId);
    }
}
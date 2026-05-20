using Microsoft.EntityFrameworkCore;
using AutoHub.API.Models;

namespace AutoHub.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<PartRequest> PartRequests { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;


    public DbSet<ServiceHistory> ServiceHistories { get; set; } = null!;

 
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleItem> SaleItems { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;

  
    public DbSet<Part> Parts { get; set; } = null!;
    public DbSet<Credit> Credits { get; set; } = null!;
    public DbSet<Vendor> Vendors { get; set; } = null!;
    public DbSet<Staff> Staffs { get; set; } = null!;
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; } = null!;
    public DbSet<SystemNotification> SystemNotifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //  PostgreSQL lowercase table convention (remove if using SQL Server)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            entity.SetTableName(entity.GetTableName()?.ToLower());

        //  UNIQUE INDEXES
        modelBuilder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(v => v.LicensePlate).IsUnique();
        modelBuilder.Entity<Staff>().HasIndex(s => s.Email).IsUnique();

        //  CONFIGURATIONS
        ConfigureStaff(modelBuilder);
        ConfigureMonetaryPrecision(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureServiceHistory(modelBuilder); 
    }

    private void ConfigureStaff(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Email).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Phone).HasMaxLength(20);
            entity.Property(s => s.Role).HasMaxLength(50).HasDefaultValue("Staff");
            entity.Property(s => s.Status).HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(s => s.Photo).HasMaxLength(500);
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
        });
    }

    private void ConfigureMonetaryPrecision(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().Property(c => c.TotalSpent).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.FinalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<SaleItem>().Property(si => si.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Part>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<PurchaseInvoice>().Property(p => p.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<PurchaseInvoice>().Property(p => p.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Credit>().Property(c => c.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Invoice>().Property(i => i.FinalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceItem>().Property(ii => ii.UnitPrice).HasPrecision(18, 2);
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        //  Customer  Child Entities (Cascade Delete)
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Customer).WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CustomerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Customer).WithMany(c => c.Appointments)
            .HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PartRequest>()
            .HasOne(p => p.Customer).WithMany(c => c.PartRequests)
            .HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Customer).WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sale>()
            .HasOne(t => t.Customer).WithMany(c => c.Sales)
            .HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Cascade);

        //  Sale → SaleItems
        modelBuilder.Entity<SaleItem>()
            .HasOne(ti => ti.Sale).WithMany(t => t.Items)
            .HasForeignKey(ti => ti.SaleId).OnDelete(DeleteBehavior.Cascade);

        //  Invoice → InvoiceItems
        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Invoice).WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId).OnDelete(DeleteBehavior.Cascade);

        //  InvoiceItem → Part (Optional, SetNull on delete)
        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Part).WithMany()
            .HasForeignKey(ii => ii.PartId).OnDelete(DeleteBehavior.SetNull);

        //  Credit → Customer (Optional, SetNull on delete)
        modelBuilder.Entity<Credit>()
            .HasOne(c => c.Customer).WithMany()
            .HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.SetNull);
    }

    
    private void ConfigureServiceHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceHistory>(entity =>
        {
            //  EXPLICIT TABLE NAME - prevents rename vs create confusion
            entity.ToTable("servicehistories");

            //  Optional: Configure key explicitly (usually not needed)
            entity.HasKey(sh => sh.Id);

            //  Relationship: ServiceHistory → Vehicle (Many-to-One)
            entity.HasOne(sh => sh.Vehicle)
                .WithMany(v => v.ServiceHistories)
                .HasForeignKey(sh => sh.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
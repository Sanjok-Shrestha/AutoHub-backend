using AutoHub.VehiclePartsAPI.Models;
using Microsoft.EntityFrameworkCore;
using VehicleManagementSystem.VehiclePartsAPI.Models;

namespace VehicleManagementSystem.VehiclePartsAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Part> Parts { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Credit> Credits { get; set; }
        public DbSet<Vendor> Vendors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Part>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Sale>()
                .Property(s => s.FinalAmount)
                .HasPrecision(18, 2);
        }
    }
}
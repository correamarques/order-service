using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.Total).HasPrecision(18, 2);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasMany(e => e.Items)
                    .WithOne()
                    .HasForeignKey("OrderId")
                    .IsRequired();

                entity.ToTable("Orders");
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.OrderId).IsRequired();
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();

                entity.ToTable("OrderItems");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.AvailableQuantity).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasIndex(e => e.Name).IsUnique();

                entity.ToTable("Products");
            });
        }
    }
}

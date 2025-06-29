using Microsoft.EntityFrameworkCore;
using ShoppingBasketAPI.Models;

namespace ShoppingBasketAPI.Data
{
    public class ShoppingBasketContext : DbContext
    {
        public ShoppingBasketContext(DbContextOptions<ShoppingBasketContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<BasketItem> BasketItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(50);
            });

            // Basket configuration
            modelBuilder.Entity<Basket>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DiscountCode).HasMaxLength(50);
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.ShippingCountry).HasMaxLength(100);
                entity.Property(e => e.ShippingCost).HasColumnType("decimal(18,2)");
                
                entity.HasIndex(e => e.SessionId).IsUnique();
            });

            // BasketItem configuration
            modelBuilder.Entity<BasketItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                
                entity.HasOne(e => e.Basket)
                    .WithMany(e => e.Items)
                    .HasForeignKey(e => e.BasketId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasIndex(e => new { e.BasketId, e.ProductId }).IsUnique();
            });
        }
    }
} 
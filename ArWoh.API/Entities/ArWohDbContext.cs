using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Entities;

public class ArWohDbContext : DbContext
{
    public ArWohDbContext(DbContextOptions<ArWohDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<AdminAction> AdminActions { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User email unique constraint
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Image orientation lưu dưới dạng string
        modelBuilder.Entity<Image>()
            .Property(i => i.Orientation)
            .HasConversion<string>();

        // Order status lưu dưới dạng string
        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>();

        // ShippingStatus lưu dưới dạng string
        modelBuilder.Entity<Order>()
            .Property(o => o.ShippingStatus)
            .HasConversion<string>();

        // Payment gateway và status lưu dưới dạng string
        modelBuilder.Entity<Payment>()
            .Property(p => p.PaymentGateway)
            .HasConversion<string>();

        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<string>();

        // Order-Customer relationship
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // OrderDetail relationships
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Image)
            .WithMany()
            .HasForeignKey(od => od.ImageId)
            .OnDelete(DeleteBehavior.Restrict);

        // Payment-Order relationship
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Order)
            .WithMany(o => o.Payments)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // AdminAction configuration
        modelBuilder.Entity<AdminAction>()
            .HasOne(a => a.Admin)
            .WithMany()
            .HasForeignKey(a => a.AdminId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Image>()
            .Property(i => i.Orientation)
            .HasConversion<string>();
    }
}
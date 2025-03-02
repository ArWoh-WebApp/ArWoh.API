using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Entities;

public class ArWohDbContext : DbContext
{
    public ArWohDbContext(DbContextOptions<ArWohDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<AdminAction> AdminActions { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Image>()
            .Property(i => i.Orientation)
            .HasConversion<string>();

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(t => t.Customer)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(t => t.Image)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.ImageId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<Order>()
            .HasOne(o => o.Transaction)
            .WithOne()
            .HasForeignKey<Order>(o => o.TransactionId);

        modelBuilder.Entity<AdminAction>()
            .HasOne(a => a.Admin)
            .WithMany()
            .HasForeignKey(a => a.AdminId);
    }
}
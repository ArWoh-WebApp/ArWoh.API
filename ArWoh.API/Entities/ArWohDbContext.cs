using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Entities;


public class ArWohDbContext : DbContext
{
    public ArWohDbContext(DbContextOptions<ArWohDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<AdminAction> AdminActions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Customer)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
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



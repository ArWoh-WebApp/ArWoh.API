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
            
        
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>(); // Lưu dưới dạng chuỗi

        // Transaction -> User relationship (Prevent cascade delete)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);  // FIX: Change cascade delete to Restrict

        // Transaction -> Image relationship (Allow cascade delete)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Image)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.ImageId)
            .OnDelete(DeleteBehavior.Cascade);  // Image deletion should still remove transactions

        modelBuilder.Entity<AdminAction>()
            .HasOne(a => a.Admin)
            .WithMany()
            .HasForeignKey(a => a.AdminId);
    }

}

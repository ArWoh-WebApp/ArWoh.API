using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Ultils;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly ArWohDbContext _context;
    private readonly ILoggerService _logger;

    public SystemController(ArWohDbContext context, ILoggerService logger)
    {
        _context = context;
        _logger = logger;
    }


    [HttpPost("seed-all-data")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await ClearDatabase(_context);
            var users = await SeedUsers();

            return Ok(ApiResult<object>.Success(new
            {
                UserEmails = users.Select(u => u.Email)
            }));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.Error($"Database update error: {dbEx.Message}");
            return StatusCode(500, "Error seeding data: Database issue.");
        }
        catch (Exception ex)
        {
            _logger.Error($"General error: {ex.Message}");
            return StatusCode(500, "Error seeding data: General failure.");
        }
    }


    private async Task<List<User>> SeedUsers()
    {
        _logger.Info("Seeding users into the database...");

        var existingUsers = await _context.Users.AnyAsync();
        if (existingUsers)
        {
            _logger.Warn("Users already exist. Skipping seeding.");
            return await _context.Users.ToListAsync();
        }

        var passwordHasher = new PasswordHasher();

        var users = new List<User>
        {
            new User
            {
                Username = "customer_user",
                Email = "customer@example.com",
                PasswordHash = passwordHasher.HashPassword("1@"),
                Role = UserRole.Customer,
                Bio = "A passionate art lover.",
                ProfilePictureUrl = "https://example.com/images/customer.png"
            },
            new User
            {
                Username = "photographer_user",
                Email = "photographer@example.com",
                PasswordHash = passwordHasher.HashPassword("1@"),
                Role = UserRole.Photographer,
                Bio = "Professional nature photographer.",
                ProfilePictureUrl = "https://example.com/images/photographer.png"
            },
            new User
            {
                Username = "admin_user",
                Email = "admin@example.com",
                PasswordHash = passwordHasher.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                Bio = "Administrator of the platform.",
                ProfilePictureUrl = "https://example.com/images/admin.png"
            }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        _logger.Success("User seeding completed successfully.");

        return users;
    }


    private async Task ClearDatabase(ArWohDbContext context)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            _logger.Info("Bắt đầu xóa dữ liệu trong database...");

            // Danh sách các bảng cần xóa theo thứ tự FK
            var tablesToDelete = new List<Func<Task>>
            {
                async () => await context.AdminActions.ExecuteDeleteAsync(),
                async () => await context.Orders.ExecuteDeleteAsync(),
                async () => await context.Transactions.ExecuteDeleteAsync(),
                async () => await context.Images.ExecuteDeleteAsync(),
                async () => await context.Users.ExecuteDeleteAsync()
            };

            // Xóa dữ liệu từng bảng theo thứ tự
            foreach (var deleteFunc in tablesToDelete)
            {
                await deleteFunc();
            }

            await transaction.CommitAsync();
            _logger.Success("Xóa sạch dữ liệu trong database thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error($"Xóa dữ liệu thất bại: {ex.Message}");
            throw;
        }
    }
}
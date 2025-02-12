using ArWoh.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArWoh.API.Architechture;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app, ILogger _logger)
    {
        try
        {
            _logger.LogInformation("Applying migrations...");
            using IServiceScope scope = app.ApplicationServices.CreateScope();

            using ArWohDbContext dbContext =
                scope.ServiceProvider.GetRequiredService<ArWohDbContext>();

            dbContext.Database.Migrate();
            _logger.LogInformation("Migrations applied successfully!");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An problem occurred during migration!");
        }
    }
}
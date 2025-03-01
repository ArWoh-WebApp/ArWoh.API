using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Seed data vào database
    /// </summary>
    /// <returns></returns>
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
            var images = await SeedImages();

            return Ok(ApiResult<object>.Success(new
            {
                UserCount = users.Count,
                ImageCount = images.Count
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
            new()
            {
                Username = "customer_user",
                Email = "customer@gmail.com",
                PasswordHash = passwordHasher.HashPassword("1@"),
                Role = UserRole.Customer,
                Bio = "A passionate art lover.",
                ProfilePictureUrl = "https://example.com/images/customer.png"
            },
            new()
            {
                Username = "photographer_user",
                Email = "photographer@gmail.com",
                PasswordHash = passwordHasher.HashPassword("1@"),
                Role = UserRole.Photographer,
                Bio = "Professional nature photographer.",
                ProfilePictureUrl = "https://example.com/images/photographer.png"
            },
            new()
            {
                Username = "admin_user",
                Email = "admin@gmail.com",
                PasswordHash = passwordHasher.HashPassword("1@"),
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

    private async Task<List<Image>> SeedImages()
    {
        var images = new List<Image>
        {
            new()
            {
                Title = "Mountain Stream at Dawn",
                Description =
                    "Captured during the early hours of dawn, this mountain stream represents the raw beauty of untouched nature. The interplay of light and shadow creates a mesmerizing scene that invites viewers to pause and reflect on the tranquility of the natural world.",
                Url = "https://images.unsplash.com/photo-1548679847-1d4ff48016c7",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Water Stream", "Brook", "Natural Water", "Landscape", "Mountains" },
                Location = "Rocky Mountains, Colorado",
                Price = 2500000,
                FileName = "mountain_stream_dawn.jpg",
                StoryOfArt = "A peaceful moment captured during dawn, highlighting nature’s untouched beauty."
            },
            new()
            {
                Title = "City Skyline at Dusk",
                Description =
                    "As daylight fades, the city transforms into a vibrant canvas of twinkling lights and urban silhouettes. The dusk creates a dramatic contrast between the illuminated skyscrapers and the darkening sky.",
                Url =
                    "https://images.unsplash.com/photo-1502635994848-2eb3b4a38201?q=80&w=1964&auto=format&fit=crop&ixlib=rb-4" +
                    ".0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Water Stream", "Brook", "Natural Water", "Landscape", "Mountains" },
                Location = "Rocky Mountains, Colorado",
                Price = 2500000,
                FileName = "mountain_stream_dawn.jpg",
                StoryOfArt =
                    "As daylight fades, the city transforms into a vibrant canvas of twinkling lights and urban silhouettes. The dusk creates a dramatic contrast between the illuminated skyscrapers and the darkening sky."
            },
            new()
            {
                Title = "Forest Trail in Autumn",
                Description =
                    "Captured during the early hours of dawn, this mountain stream represents the raw beauty of untouched nature. The interplay of light and shadow creates a mesmerizing scene that invites viewers to pause and reflect on the tranquility of the natural world.",
                Url = "https://images.unsplash.com/photo-1548679847-1d4ff48016c7",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Forest", "Autumn", "Trail", "Nature" },
                Location = "Bavarian Forest, Germany",
                Price = 2500000,
                FileName = "mountain_stream_dawn.jpg",
                StoryOfArt =
                    "The forest comes alive with vibrant hues of red, orange, and yellow. A winding trail invites wanderers to explore the crisp, refreshing atmosphere of autumn."
            },
            new()
            {
                Title = "Desert Mirage",
                Description =
                    "In the midst of arid landscapes, the shimmering heat gives rise to optical illusions that evoke mystery and wonder, capturing the stark beauty of the desert.",
                Url =
                    "https://plus.unsplash.com/premium_photo-1673631128794-e9758e20e5a8?q=80&w=1888&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Desert", "Mirage", "Heat", "Landscape" },
                Location = "Sahara Desert, Africa",
                Price = 1000000,
                FileName = "desert_mirage.jpg",
                StoryOfArt =
                    "The illusionary effect of heat waves in the vast desert creates an ethereal scene, mesmerizing travelers."
            },
            new()
            {
                Title = "Ocean Waves Crashing",
                Description =
                    "The raw power of the ocean is beautifully frozen in time as colossal waves break against rugged cliffs, showcasing nature’s unbridled energy and grace.",
                Url =
                    "https://images.unsplash.com/photo-1474767821094-a8fe9d8c8fdd?q=80&w=1740&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Ocean", "Waves", "Seascape", "Nature" },
                Location = "Big Sur, California",
                Price = 1500000,
                FileName = "ocean_waves_crashing.jpg",
                StoryOfArt = "A moment capturing the fierce yet graceful power of ocean waves."
            },
            new()
            {
                Title = "Snowy Mountain Peaks",
                Description =
                    "Towering peaks dusted with pristine snow stand as a testament to nature’s grandeur. The crisp air and brilliant sky amplify the serene yet powerful landscape.",
                Url =
                    "https://images.unsplash.com/photo-1558089551-95d707e6c13c?q=80&w=1887&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Mountains", "Snow", "Alps", "Winter" },
                Location = "Swiss Alps, Switzerland",
                Price = 2000000,
                FileName = "snowy_mountain_peaks.jpg",
                StoryOfArt = "A breathtaking view of snowy peaks, symbolizing the raw and untouched beauty of nature."
            },
            new()
            {
                Title = "Vibrant Flower Garden",
                Description =
                    "Lush, blooming flowers create a lively mosaic of colors and shapes. The garden exudes energy and beauty, inviting onlookers to lose themselves in its vibrant details.",
                Url =
                    "https://images.unsplash.com/photo-1428908728789-d2de25dbd4e2?q=80&w=1740&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Flowers", "Garden", "Colorful", "Nature" },
                Location = "Provence, France",
                Price = 2300000,
                FileName = "vibrant_flower_garden.jpg",
                StoryOfArt = "An explosion of colors and life captured in a vibrant flower garden."
            },
            new()
            {
                Title = "Rainy City Street",
                Description =
                    "Rain-soaked streets reflect neon signs and city lights, creating an atmospheric urban landscape. The scene captures the reflective mood and charm of a rainy evening.",
                Url =
                    "https://images.unsplash.com/photo-1503348379917-758650634df4?q=80&w=1887&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Rain", "City", "Urban", "Night" },
                Location = "Tokyo, Japan",
                Price = 5000000,
                FileName = "rainy_city_street.jpg",
                StoryOfArt =
                    "An urban dreamscape, where neon lights and rain create an artistic blend of colors and reflections."
            },
            new()
            {
                Title = "Sunset Over the Lake",
                Description =
                    "The sky bursts into a palette of warm colors as the sun sets, casting a gentle glow over a tranquil lake. The peaceful scene invites relaxation and introspection.",
                Url =
                    "https://images.unsplash.com/photo-1514975440715-7b6852af4ee7?q=80&w=1740&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Sunset", "Lake", "Reflection", "Nature" },
                Location = "Lake Tahoe, USA",
                Price = 2400000,
                FileName = "sunset_over_the_lake.jpg",
                StoryOfArt = "A peaceful moment reflecting the golden hues of the setting sun over a tranquil lake."
            },
            new()
            {
                Title = "Starry Night Sky",
                Description =
                    "Under the cloak of night, countless stars glitter against the deep blue backdrop. The image captures the infinite expanse of the universe, inspiring wonder and curiosity.",
                Url =
                    "https://images.unsplash.com/photo-1595246965570-9684145def50?q=80&w=1887&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Night", "Stars", "Astrophotography", "Universe" },
                Location = "Atacama Desert, Chile",
                Price = 4000000,
                FileName = "starry_night_sky.jpg",
                StoryOfArt = "A gateway to the cosmos, capturing the mesmerizing beauty of the night sky."
            },
            new()
            {
                Title = "Countryside Road",
                Description =
                    "The rustic charm of the countryside is on full display with a solitary road stretching between rolling fields and quaint farmhouses. The image evokes a sense of freedom and simplicity.",
                Url =
                    "https://images.unsplash.com/photo-1499796683658-b659bc751db1?q=80&w=1974&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Countryside", "Road", "Farms", "Landscape" },
                Location = "Tuscany, Italy",
                Price = 4000000,
                FileName = "countryside_road.jpg",
                StoryOfArt = "A journey through rolling hills, where time slows down and nature takes center stage."
            },
            new()
            {
                Title = "Ancient Castle Ruins",
                Description =
                    "Weathered stone walls and crumbling towers speak of bygone eras. The dramatic sky adds to the eerie charm of these forgotten castle ruins.",
                Url =
                    "https://images.unsplash.com/photo-1482938289607-e9573fc25ebb?q=80&w=1887&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Castle", "Ruins", "History", "Mystery" },
                Location = "Scotland, UK",
                Price = 4000000,
                FileName = "ancient_castle_ruins.jpg",
                StoryOfArt =
                    "A glimpse into the past, where the echoes of history whisper through the crumbling stones."
            },
            new()
            {
                Title = "Lush Tropical Forest",
                Description =
                    "Dense foliage and a riot of green hues define this tropical forest. The light filters through the canopy, creating a magical interplay of shadow and brightness.",
                Url =
                    "https://images.unsplash.com/photo-1451187580459-43490279c0fa?q=80&w=1744&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Tropical", "Forest", "Green", "Nature" },
                Location = "Costa Rica",
                Price = 4000000,
                FileName = "lush_tropical_forest.jpg",
                StoryOfArt = "A hidden paradise, where nature flourishes in an explosion of green."
            },
            new()
            {
                Title = "Calm River Bend",
                Description =
                    "A meandering river reflects the soft hues of a fading day. The scene exudes tranquility and invites viewers to savor the simplicity of nature's flow.",
                Url =
                    "https://images.unsplash.com/photo-1483959651481-dc75b89291f1?q=80&w=1849&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "River", "Calm", "Landscape", "Nature" },
                Location = "Loire Valley, France",
                Price = 4000000,
                FileName = "calm_river_bend.jpg",
                StoryOfArt = "A peaceful waterway winding through nature’s quiet embrace."
            },
            new()
            {
                Title = "Modern Architecture",
                Description =
                    "This image highlights the intersection of art and engineering. Clean geometric forms and expansive glass surfaces define a modern structure set against a clear sky.",
                Url =
                    "https://images.unsplash.com/photo-1518599904199-0ca897819ddb?q=80&w=1734&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Architecture", "Modern", "Design", "Urban" },
                Location = "Dubai, UAE",
                Price = 4000000,
                FileName = "modern_architecture.jpg",
                StoryOfArt = "A vision of the future, where form meets function in architectural brilliance."
            },
            new()
            {
                Title = "Misty Morning in the Valley",
                Description =
                    "Early morning mist envelops the valley, softening the landscape and lending an ethereal quality to the rolling hills and scattered trees. The quiet ambiance is both soothing and mysterious.",
                Url =
                    "https://images.unsplash.com/photo-1582562478517-6f88924e668d?q=80&w=1827&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Mist", "Morning", "Valley", "Nature" },
                Location = "Yosemite Valley, USA",
                Price = 4000000,
                FileName = "misty_morning_valley.jpg",
                StoryOfArt = "A magical morning where mist dances over the rolling hills."
            }
        };

        await _context.Images.AddRangeAsync(images);
        await _context.SaveChangesAsync();

        return images;
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
            foreach (var deleteFunc in tablesToDelete) await deleteFunc();

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
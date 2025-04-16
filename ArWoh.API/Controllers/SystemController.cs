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
    ///     Seed data vào database
    /// </summary>
    [HttpPost("seed-all-data")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await ClearDatabase(_context);
            await SeedUsersAndImages();

            return Ok(ApiResult<object>.Success("Seeding completed successfully"));
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

    private async Task SeedUsersAndImages()
    {
        _logger.Info("Seeding users and images into the database...");

        if (await _context.Users.AnyAsync())
        {
            _logger.Warn("Users already exist. Skipping seeding.");
            return;
        }

        var passwordHasher = new PasswordHasher();

        var users = new List<User>
        {
            new()
            {
                Username = "Anh Tiến Kendu",
                Email = "user1@gmail.com",
                PasswordHash = passwordHasher.HashPassword("1@"),
                Role = UserRole.Customer,
                Bio = "Một coder biết chơi đàn và thích chụp ảnh, mê đi phượt và rất yêu mèo.",
                ProfilePictureUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=user-avatars%2F81700597_1303744603146435_5830943911396245504_n.jpg&version_id=null"
            },
            new()
            {
                Username = "Tiến Nhiếp Ảnh Gia",
                Email = "hoangtien1105@gmail.com",
                PasswordHash = passwordHasher.HashPassword("hoangtien1105"),
                Role = UserRole.Photographer,
                Bio = "Một coder biết chơi đàn và thích chụp ảnh, mê đi phượt và rất yêu mèo.",
                ProfilePictureUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=user-avatars%2F81700597_1303744603146435_5830943911396245504_n.jpg&version_id=null"
            },
            new()
            {
                Username = "Andy Nguyễn",
                Email = "a@gmail.com",
                PasswordHash = passwordHasher.HashPassword("a"),
                Role = UserRole.Photographer,
                Bio = "Ca sĩ nhưng thích chụp hình",
                ProfilePictureUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=user-avatars%2Fandynguyen.jpg&version_id=null"
            },
            new()
            {
                Username = "Nguyễn Á",
                Email = "nguyena@gmail.com",
                PasswordHash = passwordHasher.HashPassword("a"),
                Role = UserRole.Photographer,
                Bio = "Chụp hình và chụp hình",
                ProfilePictureUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=user-avatars%2Fblabla.jpg&version_id=null"
            },
            new()
            {
                Username = "admin_user",
                Email = "admin@gmail.com",
                PasswordHash = passwordHasher.HashPassword("1@"),
                Role = UserRole.Admin,
                Bio = "Administrator of the platform.",
                ProfilePictureUrl = "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=user-avatars%2F8_2ccb8671-267a-4f7c-baf9-031bb58f339b.png&version_id=null"
            }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var photographers = await _context.Users
            .Where(u => u.Role == UserRole.Photographer)
            .Select(u => u.Id)
            .ToListAsync();

        if (!photographers.Any())
        {
            _logger.Error("No photographers found. Skipping image seeding.");
            return;
        }

        var random = new Random();

        var images = new List<Image>
        {
            #region CHÙA BÀ THIÊN HẬU

            new()
            {
                Title = "Desert Dunes at Sunset",
                Description =
                    "Rolling sand dunes catch the last golden rays of sunset, creating a mesmerizing pattern of light and shadow across the desert landscape.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FChuaBa%2FIMG_2318.jpg&version_id=null",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Desert", "Dunes", "Sunset", "Sand", "Minimalist" },
                Location = "Sahara Desert, Morocco",
                Price = 35000,
                FileName = "desert_dunes_sunset.jpg",
                StoryOfArt =
                    "✨MÁI NGÓI RÊU PHONG - HƯƠNG TRẦM MÊNH MÔNG✨\r\nNép mình giữa lòng Sài Gòn nhộn nhịp, Chùa Bà Thiên Hậu là một biểu tượng văn hóa lâu đời của cộng đồng người Hoa, nơi những giá trị truyền thống vẫn được gìn giữ qua bao thế hệ. Với kiến trúc cổ kính, mái ngói rêu phong, những vòng nhang xoay tròn trong không gian trầm mặc, ngôi chùa không chỉ là chốn tâm linh mà còn là một bức tranh sống động của lịch sử và nghệ thuật.\r\nLấy cảm hứng từ vẻ đẹp huyền bí của ngôi chùa, bộ sưu tập mới sẽ tái hiện những khoảnh khắc giao thoa giữa quá khứ và hiện tại. Khói hương trầm mặc, ánh sáng len lỏi qua mái ngói, và những hoa văn chạm trổ tinh xảo – tất cả sẽ được chuyển tải thành câu chuyện đầy cảm xúc.",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },
            new()
            {
                Title = "Autumn Forest Path",
                Description =
                    "A winding path cuts through a forest ablaze with autumn colors, creating a tunnel of golden and crimson foliage.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FChuaBa%2FIMG_2318.jpg&version_id=null",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Autumn", "Forest", "Path", "Fall Colors", "Trees" },
                Location = "Vermont, USA",
                Price = 28000,
                FileName = "autumn_forest_path.jpg",
                StoryOfArt =
                    "✨MÁI NGÓI RÊU PHONG - HƯƠNG TRẦM MÊNH MÔNG✨\r\nNép mình giữa lòng Sài Gòn nhộn nhịp, Chùa Bà Thiên Hậu là một biểu tượng văn hóa lâu đời của cộng đồng người Hoa, nơi những giá trị truyền thống vẫn được gìn giữ qua bao thế hệ. Với kiến trúc cổ kính, mái ngói rêu phong, những vòng nhang xoay tròn trong không gian trầm mặc, ngôi chùa không chỉ là chốn tâm linh mà còn là một bức tranh sống động của lịch sử và nghệ thuật.\r\nLấy cảm hứng từ vẻ đẹp huyền bí của ngôi chùa, bộ sưu tập mới sẽ tái hiện những khoảnh khắc giao thoa giữa quá khứ và hiện tại. Khói hương trầm mặc, ánh sáng len lỏi qua mái ngói, và những hoa văn chạm trổ tinh xảo – tất cả sẽ được chuyển tải thành câu chuyện đầy cảm xúc.",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },
            new()
            {
                Title = "Coastal Cliff Sunrise",
                Description =
                    "Dramatic sea cliffs catch the first light of day as waves crash against their base, showcasing the raw power of where land meets sea.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FChuaBa%2FIMG_2320.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Coast", "Cliffs", "Ocean", "Sunrise", "Waves" },
                Location = "Moher, Ireland",
                Price = 45000,
                FileName = "coastal_cliff_sunrise.jpg",
                StoryOfArt =
                    "✨MÁI NGÓI RÊU PHONG - HƯƠNG TRẦM MÊNH MÔNG✨\r\nNép mình giữa lòng Sài Gòn nhộn nhịp, Chùa Bà Thiên Hậu là một biểu tượng văn hóa lâu đời của cộng đồng người Hoa, nơi những giá trị truyền thống vẫn được gìn giữ qua bao thế hệ. Với kiến trúc cổ kính, mái ngói rêu phong, những vòng nhang xoay tròn trong không gian trầm mặc, ngôi chùa không chỉ là chốn tâm linh mà còn là một bức tranh sống động của lịch sử và nghệ thuật.\r\nLấy cảm hứng từ vẻ đẹp huyền bí của ngôi chùa, bộ sưu tập mới sẽ tái hiện những khoảnh khắc giao thoa giữa quá khứ và hiện tại. Khói hương trầm mặc, ánh sáng len lỏi qua mái ngói, và những hoa văn chạm trổ tinh xảo – tất cả sẽ được chuyển tải thành câu chuyện đầy cảm xúc.",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },
            new()
            {
                Title = "Mountain Lake Reflection",
                Description =
                    "A pristine alpine lake perfectly mirrors the surrounding mountains and sky, creating a symmetrical landscape of extraordinary beauty.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FChuaBa%2FIMG_2321.jpg&version_id=null",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Lake", "Mountains", "Reflection", "Alpine", "Symmetry" },
                Location = "Banff National Park, Canada",
                Price = 38000,
                FileName = "mountain_lake_reflection.jpg",
                StoryOfArt =
                    "✨MÁI NGÓI RÊU PHONG - HƯƠNG TRẦM MÊNH MÔNG✨\r\nNép mình giữa lòng Sài Gòn nhộn nhịp, Chùa Bà Thiên Hậu là một biểu tượng văn hóa lâu đời của cộng đồng người Hoa, nơi những giá trị truyền thống vẫn được gìn giữ qua bao thế hệ. Với kiến trúc cổ kính, mái ngói rêu phong, những vòng nhang xoay tròn trong không gian trầm mặc, ngôi chùa không chỉ là chốn tâm linh mà còn là một bức tranh sống động của lịch sử và nghệ thuật.\r\nLấy cảm hứng từ vẻ đẹp huyền bí của ngôi chùa, bộ sưu tập mới sẽ tái hiện những khoảnh khắc giao thoa giữa quá khứ và hiện tại. Khói hương trầm mặc, ánh sáng len lỏi qua mái ngói, và những hoa văn chạm trổ tinh xảo – tất cả sẽ được chuyển tải thành câu chuyện đầy cảm xúc.",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },
            new()
            {
                Title = "Urban Night Rain",
                Description =
                    "A busy city intersection glistens with reflections from neon signs and traffic lights, as rain transforms an ordinary urban scene into a colorful abstract canvas.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FChuaBa%2FIMG_2321.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Urban", "Rain", "Night", "City Lights", "Reflection" },
                Location = "Seoul, South Korea",
                Price = 42000,
                FileName = "urban_night_rain.jpg",
                StoryOfArt =
                    "✨MÁI NGÓI RÊU PHONG - HƯƠNG TRẦM MÊNH MÔNG✨\r\nNép mình giữa lòng Sài Gòn nhộn nhịp, Chùa Bà Thiên Hậu là một biểu tượng văn hóa lâu đời của cộng đồng người Hoa, nơi những giá trị truyền thống vẫn được gìn giữ qua bao thế hệ. Với kiến trúc cổ kính, mái ngói rêu phong, những vòng nhang xoay tròn trong không gian trầm mặc, ngôi chùa không chỉ là chốn tâm linh mà còn là một bức tranh sống động của lịch sử và nghệ thuật.\r\nLấy cảm hứng từ vẻ đẹp huyền bí của ngôi chùa, bộ sưu tập mới sẽ tái hiện những khoảnh khắc giao thoa giữa quá khứ và hiện tại. Khói hương trầm mặc, ánh sáng len lỏi qua mái ngói, và những hoa văn chạm trổ tinh xảo – tất cả sẽ được chuyển tải thành câu chuyện đầy cảm xúc.",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },
            new()
            {
                Title = "Ancient Olive Grove",
                Description =
                    "Centuries-old olive trees with gnarled trunks stand in formation across a Mediterranean hillside, their silver-green leaves shimmering in the warm light.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FChuaBa%2FIMG_2329.jpg&version_id=null",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Olives", "Trees", "Mediterranean", "Agriculture", "Ancient" },
                Location = "Puglia, Italy",
                Price = 32000,
                FileName = "ancient_olive_grove.jpg",
                StoryOfArt =
                    "✨MÁI NGÓI RÊU PHONG - HƯƠNG TRẦM MÊNH MÔNG✨\r\nNép mình giữa lòng Sài Gòn nhộn nhịp, Chùa Bà Thiên Hậu là một biểu tượng văn hóa lâu đời của cộng đồng người Hoa, nơi những giá trị truyền thống vẫn được gìn giữ qua bao thế hệ. Với kiến trúc cổ kính, mái ngói rêu phong, những vòng nhang xoay tròn trong không gian trầm mặc, ngôi chùa không chỉ là chốn tâm linh mà còn là một bức tranh sống động của lịch sử và nghệ thuật.\r\nLấy cảm hứng từ vẻ đẹp huyền bí của ngôi chùa, bộ sưu tập mới sẽ tái hiện những khoảnh khắc giao thoa giữa quá khứ và hiện tại. Khói hương trầm mặc, ánh sáng len lỏi qua mái ngói, và những hoa văn chạm trổ tinh xảo – tất cả sẽ được chuyển tải thành câu chuyện đầy cảm xúc.",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },
            new()
            {
                Title = "Ancient Olive Grove",
                Description =
                    "Centuries-old olive trees with gnarled trunks stand in formation across a Mediterranean hillside, their silver-green leaves shimmering in the warm light.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FChuaBa%2FIMG_2329.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Olives", "Trees", "Mediterranean", "Agriculture", "Ancient" },
                Location = "Puglia, Italy",
                Price = 32000,
                FileName = "ancient_olive_grove.jpg",
                StoryOfArt =
                    "✨MÁI NGÓI RÊU PHONG - HƯƠNG TRẦM MÊNH MÔNG✨\r\nNép mình giữa lòng Sài Gòn nhộn nhịp, Chùa Bà Thiên Hậu là một biểu tượng văn hóa lâu đời của cộng đồng người Hoa, nơi những giá trị truyền thống vẫn được gìn giữ qua bao thế hệ. Với kiến trúc cổ kính, mái ngói rêu phong, những vòng nhang xoay tròn trong không gian trầm mặc, ngôi chùa không chỉ là chốn tâm linh mà còn là một bức tranh sống động của lịch sử và nghệ thuật.\r\nLấy cảm hứng từ vẻ đẹp huyền bí của ngôi chùa, bộ sưu tập mới sẽ tái hiện những khoảnh khắc giao thoa giữa quá khứ và hiện tại. Khói hương trầm mặc, ánh sáng len lỏi qua mái ngói, và những hoa văn chạm trổ tinh xảo – tất cả sẽ được chuyển tải thành câu chuyện đầy cảm xúc.",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            #endregion

            #region METRO

            new()
            {
                Title = "Nhịp Sống Metro Sài Gòn",
                Description =
                    "Tuyến tàu điện ngầm đầu tiên của Sài Gòn hiện lên sống động, biểu tượng cho sự phát triển đô thị hiện đại song hành cùng nhịp sống năng động của thành phố.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FMetro%2FIMG_5722-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Metro", "Sài Gòn", "Đô thị", "Giao thông", "Hiện đại" },
                Location = "Tuyến Metro Số 1, TP Hồ Chí Minh, Việt Nam",
                Price = 42000,
                FileName = "saigon_metro_line1.jpg",
                StoryOfArt =
                    "🚆 COLLECTION: Metro Sài Gòn – Nhịp đập hiện đại giữa lòng thành phố\r\nGiữa lòng thành phố nhộn nhịp, metro Sài Gòn vươn mình như một biểu tượng của sự đổi mới và phát triển. Những đoàn tàu xanh lam lướt nhẹ trên cao, phản chiếu ánh nắng vàng rực rỡ, bên dưới là dòng người hối hả di chuyển – một bức tranh đô thị sống động, vừa hiện đại vừa hoài niệm.\r\nTuyến metro không chỉ là phương tiện giao thông, mà còn đánh dấu một bước ngoặt trong nhịp sống Sài Gòn. Nó mang theo hy vọng về một thành phố năng động hơn, gắn kết hơn, nơi mọi hành trình đều trở nên dễ dàng và trọn vẹn.\r\n📷 Hình ảnh metro Sài Gòn qua góc nhìn nghệ thuật. Không chỉ là phương tiện, mà còn là câu chuyện về sự phát triển, kết nối và hy vọng.\r\n✨ Sở hữu ngay artwork Metro Sài Gòn tại ArWoh, nơi nghệ thuật kể lên câu chuyện của thành phố! 🚉",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            //2
            new()
            {
                Title = "Nhịp Sống Metro Sài Gòn",
                Description =
                    "Tuyến tàu điện ngầm đầu tiên của Sài Gòn hiện lên sống động, biểu tượng cho sự phát triển đô thị hiện đại song hành cùng nhịp sống năng động của thành phố.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FMetro%2FIMG_5723-min.jpg&version_id=null",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Metro", "Sài Gòn", "Đô thị", "Giao thông", "Hiện đại" },
                Location = "Tuyến Metro Số 1, TP Hồ Chí Minh, Việt Nam",
                Price = 42000,
                FileName = "saigon_metro_line1.jpg",
                StoryOfArt =
                    "🚆 COLLECTION: Metro Sài Gòn – Nhịp đập hiện đại giữa lòng thành phố\r\nGiữa lòng thành phố nhộn nhịp, metro Sài Gòn vươn mình như một biểu tượng của sự đổi mới và phát triển. Những đoàn tàu xanh lam lướt nhẹ trên cao, phản chiếu ánh nắng vàng rực rỡ, bên dưới là dòng người hối hả di chuyển – một bức tranh đô thị sống động, vừa hiện đại vừa hoài niệm.\r\nTuyến metro không chỉ là phương tiện giao thông, mà còn đánh dấu một bước ngoặt trong nhịp sống Sài Gòn. Nó mang theo hy vọng về một thành phố năng động hơn, gắn kết hơn, nơi mọi hành trình đều trở nên dễ dàng và trọn vẹn.\r\n📷 Hình ảnh metro Sài Gòn qua góc nhìn nghệ thuật. Không chỉ là phương tiện, mà còn là câu chuyện về sự phát triển, kết nối và hy vọng.\r\n✨ Sở hữu ngay artwork Metro Sài Gòn tại ArWoh, nơi nghệ thuật kể lên câu chuyện của thành phố! 🚉",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },


            new()
            {
                Title = "Nhịp Sống Metro Sài Gòn",
                Description =
                    "Tuyến tàu điện ngầm đầu tiên của Sài Gòn hiện lên sống động, biểu tượng cho sự phát triển đô thị hiện đại song hành cùng nhịp sống năng động của thành phố.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FMetro%2FIMG_5723-min.jpg&version_id=null",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Metro", "Sài Gòn", "Đô thị", "Giao thông", "Hiện đại" },
                Location = "Tuyến Metro Số 1, TP Hồ Chí Minh, Việt Nam",
                Price = 42000,
                FileName = "saigon_metro_line1.jpg",
                StoryOfArt =
                    "🚆 COLLECTION: Metro Sài Gòn – Nhịp đập hiện đại giữa lòng thành phố\r\nGiữa lòng thành phố nhộn nhịp, metro Sài Gòn vươn mình như một biểu tượng của sự đổi mới và phát triển. Những đoàn tàu xanh lam lướt nhẹ trên cao, phản chiếu ánh nắng vàng rực rỡ, bên dưới là dòng người hối hả di chuyển – một bức tranh đô thị sống động, vừa hiện đại vừa hoài niệm.\r\nTuyến metro không chỉ là phương tiện giao thông, mà còn đánh dấu một bước ngoặt trong nhịp sống Sài Gòn. Nó mang theo hy vọng về một thành phố năng động hơn, gắn kết hơn, nơi mọi hành trình đều trở nên dễ dàng và trọn vẹn.\r\n📷 Hình ảnh metro Sài Gòn qua góc nhìn nghệ thuật. Không chỉ là phương tiện, mà còn là câu chuyện về sự phát triển, kết nối và hy vọng.\r\n✨ Sở hữu ngay artwork Metro Sài Gòn tại ArWoh, nơi nghệ thuật kể lên câu chuyện của thành phố! 🚉",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },


            new()
            {
                Title = "Nhịp Sống Metro Sài Gòn",
                Description =
                    "Tuyến tàu điện ngầm đầu tiên của Sài Gòn hiện lên sống động, biểu tượng cho sự phát triển đô thị hiện đại song hành cùng nhịp sống năng động của thành phố.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FMetro%2FIMG_5724-min.jpg&version_id=null",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Metro", "Sài Gòn", "Đô thị", "Giao thông", "Hiện đại" },
                Location = "Tuyến Metro Số 1, TP Hồ Chí Minh, Việt Nam",
                Price = 42000,
                FileName = "saigon_metro_line1.jpg",
                StoryOfArt =
                    "🚆 COLLECTION: Metro Sài Gòn – Nhịp đập hiện đại giữa lòng thành phố\r\nGiữa lòng thành phố nhộn nhịp, metro Sài Gòn vươn mình như một biểu tượng của sự đổi mới và phát triển. Những đoàn tàu xanh lam lướt nhẹ trên cao, phản chiếu ánh nắng vàng rực rỡ, bên dưới là dòng người hối hả di chuyển – một bức tranh đô thị sống động, vừa hiện đại vừa hoài niệm.\r\nTuyến metro không chỉ là phương tiện giao thông, mà còn đánh dấu một bước ngoặt trong nhịp sống Sài Gòn. Nó mang theo hy vọng về một thành phố năng động hơn, gắn kết hơn, nơi mọi hành trình đều trở nên dễ dàng và trọn vẹn.\r\n📷 Hình ảnh metro Sài Gòn qua góc nhìn nghệ thuật. Không chỉ là phương tiện, mà còn là câu chuyện về sự phát triển, kết nối và hy vọng.\r\n✨ Sở hữu ngay artwork Metro Sài Gòn tại ArWoh, nơi nghệ thuật kể lên câu chuyện của thành phố! 🚉",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },


            new()
            {
                Title = "Nhịp Sống Metro Sài Gòn",
                Description =
                    "Tuyến tàu điện ngầm đầu tiên của Sài Gòn hiện lên sống động, biểu tượng cho sự phát triển đô thị hiện đại song hành cùng nhịp sống năng động của thành phố.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FMetro%2FIMG_5734-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Metro", "Sài Gòn", "Đô thị", "Giao thông", "Hiện đại" },
                Location = "Tuyến Metro Số 1, TP Hồ Chí Minh, Việt Nam",
                Price = 42000,
                FileName = "saigon_metro_line1.jpg",
                StoryOfArt =
                    "🚆 COLLECTION: Metro Sài Gòn – Nhịp đập hiện đại giữa lòng thành phố\r\nGiữa lòng thành phố nhộn nhịp, metro Sài Gòn vươn mình như một biểu tượng của sự đổi mới và phát triển. Những đoàn tàu xanh lam lướt nhẹ trên cao, phản chiếu ánh nắng vàng rực rỡ, bên dưới là dòng người hối hả di chuyển – một bức tranh đô thị sống động, vừa hiện đại vừa hoài niệm.\r\nTuyến metro không chỉ là phương tiện giao thông, mà còn đánh dấu một bước ngoặt trong nhịp sống Sài Gòn. Nó mang theo hy vọng về một thành phố năng động hơn, gắn kết hơn, nơi mọi hành trình đều trở nên dễ dàng và trọn vẹn.\r\n📷 Hình ảnh metro Sài Gòn qua góc nhìn nghệ thuật. Không chỉ là phương tiện, mà còn là câu chuyện về sự phát triển, kết nối và hy vọng.\r\n✨ Sở hữu ngay artwork Metro Sài Gòn tại ArWoh, nơi nghệ thuật kể lên câu chuyện của thành phố! 🚉",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },


            new()
            {
                Title = "Nhịp Sống Metro Sài Gòn",
                Description =
                    "Tuyến tàu điện ngầm đầu tiên của Sài Gòn hiện lên sống động, biểu tượng cho sự phát triển đô thị hiện đại song hành cùng nhịp sống năng động của thành phố.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2Fmetro%20(17).jpg&version_id=null",
                Orientation = OrientationType.Landscape,
                Tags = new List<string> { "Metro", "Sài Gòn", "Đô thị", "Giao thông", "Hiện đại" },
                Location = "Tuyến Metro Số 1, TP Hồ Chí Minh, Việt Nam",
                Price = 42000,
                FileName = "saigon_metro_line1.jpg",
                StoryOfArt =
                    "🚆 COLLECTION: Metro Sài Gòn – Nhịp đập hiện đại giữa lòng thành phố\r\nGiữa lòng thành phố nhộn nhịp, metro Sài Gòn vươn mình như một biểu tượng của sự đổi mới và phát triển. Những đoàn tàu xanh lam lướt nhẹ trên cao, phản chiếu ánh nắng vàng rực rỡ, bên dưới là dòng người hối hả di chuyển – một bức tranh đô thị sống động, vừa hiện đại vừa hoài niệm.\r\nTuyến metro không chỉ là phương tiện giao thông, mà còn đánh dấu một bước ngoặt trong nhịp sống Sài Gòn. Nó mang theo hy vọng về một thành phố năng động hơn, gắn kết hơn, nơi mọi hành trình đều trở nên dễ dàng và trọn vẹn.\r\n📷 Hình ảnh metro Sài Gòn qua góc nhìn nghệ thuật. Không chỉ là phương tiện, mà còn là câu chuyện về sự phát triển, kết nối và hy vọng.\r\n✨ Sở hữu ngay artwork Metro Sài Gòn tại ArWoh, nơi nghệ thuật kể lên câu chuyện của thành phố! 🚉",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },


            new()
            {
                Title = "Nhịp Sống Metro Sài Gòn",
                Description =
                    "Tuyến tàu điện ngầm đầu tiên của Sài Gòn hiện lên sống động, biểu tượng cho sự phát triển đô thị hiện đại song hành cùng nhịp sống năng động của thành phố.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FMetro%2FIMG_5739-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Metro", "Sài Gòn", "Đô thị", "Giao thông", "Hiện đại" },
                Location = "Tuyến Metro Số 1, TP Hồ Chí Minh, Việt Nam",
                Price = 42000,
                FileName = "saigon_metro_line1.jpg",
                StoryOfArt =
                    "🚆 COLLECTION: Metro Sài Gòn – Nhịp đập hiện đại giữa lòng thành phố\r\nGiữa lòng thành phố nhộn nhịp, metro Sài Gòn vươn mình như một biểu tượng của sự đổi mới và phát triển. Những đoàn tàu xanh lam lướt nhẹ trên cao, phản chiếu ánh nắng vàng rực rỡ, bên dưới là dòng người hối hả di chuyển – một bức tranh đô thị sống động, vừa hiện đại vừa hoài niệm.\r\nTuyến metro không chỉ là phương tiện giao thông, mà còn đánh dấu một bước ngoặt trong nhịp sống Sài Gòn. Nó mang theo hy vọng về một thành phố năng động hơn, gắn kết hơn, nơi mọi hành trình đều trở nên dễ dàng và trọn vẹn.\r\n📷 Hình ảnh metro Sài Gòn qua góc nhìn nghệ thuật. Không chỉ là phương tiện, mà còn là câu chuyện về sự phát triển, kết nối và hy vọng.\r\n✨ Sở hữu ngay artwork Metro Sài Gòn tại ArWoh, nơi nghệ thuật kể lên câu chuyện của thành phố! 🚉",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },


            new()
            {
                Title = "Nhịp Sống Metro Sài Gòn",
                Description =
                    "Tuyến tàu điện ngầm đầu tiên của Sài Gòn hiện lên sống động, biểu tượng cho sự phát triển đô thị hiện đại song hành cùng nhịp sống năng động của thành phố.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FMetro%2FIMG_5764-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Metro", "Sài Gòn", "Đô thị", "Giao thông", "Hiện đại" },
                Location = "Tuyến Metro Số 1, TP Hồ Chí Minh, Việt Nam",
                Price = 42000,
                FileName = "saigon_metro_line1.jpg",
                StoryOfArt =
                    "🚆 COLLECTION: Metro Sài Gòn – Nhịp đập hiện đại giữa lòng thành phố\r\nGiữa lòng thành phố nhộn nhịp, metro Sài Gòn vươn mình như một biểu tượng của sự đổi mới và phát triển. Những đoàn tàu xanh lam lướt nhẹ trên cao, phản chiếu ánh nắng vàng rực rỡ, bên dưới là dòng người hối hả di chuyển – một bức tranh đô thị sống động, vừa hiện đại vừa hoài niệm.\r\nTuyến metro không chỉ là phương tiện giao thông, mà còn đánh dấu một bước ngoặt trong nhịp sống Sài Gòn. Nó mang theo hy vọng về một thành phố năng động hơn, gắn kết hơn, nơi mọi hành trình đều trở nên dễ dàng và trọn vẹn.\r\n📷 Hình ảnh metro Sài Gòn qua góc nhìn nghệ thuật. Không chỉ là phương tiện, mà còn là câu chuyện về sự phát triển, kết nối và hy vọng.\r\n✨ Sở hữu ngay artwork Metro Sài Gòn tại ArWoh, nơi nghệ thuật kể lên câu chuyện của thành phố! 🚉",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            #endregion

            #region ĐẠI HỘI HUẾ

            new()
            {
                Title = "Bình Minh Tại Đại Nội Huế",
                Description =
                    "Ánh bình minh đầu ngày phủ lên các kiến trúc cổ kính của Hoàng thành Huế, tạo nên sự tương phản mạnh mẽ giữa bóng tối và ánh sáng, làm nổi bật vẻ đẹp tinh xảo của kiến trúc hoàng gia.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FHue%2F%C4%90%E1%BA%A1i%20n%E1%BB%99i-01-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Huế", "Di sản", "Kiến trúc", "Hoàng cung", "Văn hóa" },
                Location = "Đại Nội Huế, Thừa Thiên Huế, Việt Nam",
                Price = 35000,
                FileName = "dai_noi_hue_binh_minh.png",
                StoryOfArt =
                    "\"Huế là một bài thơ, một bức tranh, một bản nhạc\". \r\nKinh Thành Huế - nơi hội tụ những giá trị văn hóa truyền thống, nơi những công trình kiến trúc cổ kính kể lại câu chuyện về một thời kỳ lịch sử hào hùng của dân tộc. Những tuyệt tác kiến trúc mang vẻ đẹp cổ kính của Kinh thành là kết tinh của tinh hoa kiến trúc Việt Nam. Không chỉ là chứng tích lịch sử mà còn là biểu tượng của một nền văn hóa lâu đời, là niềm tự hào của mỗi người con đất Việt.\r\nCùng ArWoh khám phá vẻ đẹp cổ kính của cố đô qua collection đầu tiên mang tên \"triều đại\". ",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            new()
            {
                Title = "Bình Minh Tại Đại Nội Huế",
                Description =
                    "Ánh bình minh đầu ngày phủ lên các kiến trúc cổ kính của Hoàng thành Huế, tạo nên sự tương phản mạnh mẽ giữa bóng tối và ánh sáng, làm nổi bật vẻ đẹp tinh xảo của kiến trúc hoàng gia.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FHue%2F%C4%90%E1%BA%A1i%20n%E1%BB%99i-02-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Huế", "Di sản", "Kiến trúc", "Hoàng cung", "Văn hóa" },
                Location = "Đại Nội Huế, Thừa Thiên Huế, Việt Nam",
                Price = 35000,
                FileName = "dai_noi_hue_binh_minh.png",
                StoryOfArt =
                    "\"Huế là một bài thơ, một bức tranh, một bản nhạc\". \r\nKinh Thành Huế - nơi hội tụ những giá trị văn hóa truyền thống, nơi những công trình kiến trúc cổ kính kể lại câu chuyện về một thời kỳ lịch sử hào hùng của dân tộc. Những tuyệt tác kiến trúc mang vẻ đẹp cổ kính của Kinh thành là kết tinh của tinh hoa kiến trúc Việt Nam. Không chỉ là chứng tích lịch sử mà còn là biểu tượng của một nền văn hóa lâu đời, là niềm tự hào của mỗi người con đất Việt.\r\nCùng ArWoh khám phá vẻ đẹp cổ kính của cố đô qua collection đầu tiên mang tên \"triều đại\". ",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            new()
            {
                Title = "Bình Minh Tại Đại Nội Huế",
                Description =
                    "Ánh bình minh đầu ngày phủ lên các kiến trúc cổ kính của Hoàng thành Huế, tạo nên sự tương phản mạnh mẽ giữa bóng tối và ánh sáng, làm nổi bật vẻ đẹp tinh xảo của kiến trúc hoàng gia.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FHue%2F%C4%90%E1%BA%A1i%20n%E1%BB%99i-03-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Huế", "Di sản", "Kiến trúc", "Hoàng cung", "Văn hóa" },
                Location = "Đại Nội Huế, Thừa Thiên Huế, Việt Nam",
                Price = 35000,
                FileName = "dai_noi_hue_binh_minh.png",
                StoryOfArt =
                    "\"Huế là một bài thơ, một bức tranh, một bản nhạc\". \r\nKinh Thành Huế - nơi hội tụ những giá trị văn hóa truyền thống, nơi những công trình kiến trúc cổ kính kể lại câu chuyện về một thời kỳ lịch sử hào hùng của dân tộc. Những tuyệt tác kiến trúc mang vẻ đẹp cổ kính của Kinh thành là kết tinh của tinh hoa kiến trúc Việt Nam. Không chỉ là chứng tích lịch sử mà còn là biểu tượng của một nền văn hóa lâu đời, là niềm tự hào của mỗi người con đất Việt.\r\nCùng ArWoh khám phá vẻ đẹp cổ kính của cố đô qua collection đầu tiên mang tên \"triều đại\". ",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            new()
            {
                Title = "Bình Minh Tại Đại Nội Huế",
                Description =
                    "Ánh bình minh đầu ngày phủ lên các kiến trúc cổ kính của Hoàng thành Huế, tạo nên sự tương phản mạnh mẽ giữa bóng tối và ánh sáng, làm nổi bật vẻ đẹp tinh xảo của kiến trúc hoàng gia.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FHue%2F%C4%90%E1%BA%A1i%20n%E1%BB%99i-04-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Huế", "Di sản", "Kiến trúc", "Hoàng cung", "Văn hóa" },
                Location = "Đại Nội Huế, Thừa Thiên Huế, Việt Nam",
                Price = 35000,
                FileName = "dai_noi_hue_binh_minh.png",
                StoryOfArt =
                    "\"Huế là một bài thơ, một bức tranh, một bản nhạc\". \r\nKinh Thành Huế - nơi hội tụ những giá trị văn hóa truyền thống, nơi những công trình kiến trúc cổ kính kể lại câu chuyện về một thời kỳ lịch sử hào hùng của dân tộc. Những tuyệt tác kiến trúc mang vẻ đẹp cổ kính của Kinh thành là kết tinh của tinh hoa kiến trúc Việt Nam. Không chỉ là chứng tích lịch sử mà còn là biểu tượng của một nền văn hóa lâu đời, là niềm tự hào của mỗi người con đất Việt.\r\nCùng ArWoh khám phá vẻ đẹp cổ kính của cố đô qua collection đầu tiên mang tên \"triều đại\". ",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },
            //5
            new()
            {
                Title = "Bình Minh Tại Đại Nội Huế",
                Description =
                    "Ánh bình minh đầu ngày phủ lên các kiến trúc cổ kính của Hoàng thành Huế, tạo nên sự tương phản mạnh mẽ giữa bóng tối và ánh sáng, làm nổi bật vẻ đẹp tinh xảo của kiến trúc hoàng gia.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FHue%2F%C4%90%E1%BA%A1i%20n%E1%BB%99i-05-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Huế", "Di sản", "Kiến trúc", "Hoàng cung", "Văn hóa" },
                Location = "Đại Nội Huế, Thừa Thiên Huế, Việt Nam",
                Price = 35000,
                FileName = "dai_noi_hue_binh_minh.png",
                StoryOfArt =
                    "\"Huế là một bài thơ, một bức tranh, một bản nhạc\". \r\nKinh Thành Huế - nơi hội tụ những giá trị văn hóa truyền thống, nơi những công trình kiến trúc cổ kính kể lại câu chuyện về một thời kỳ lịch sử hào hùng của dân tộc. Những tuyệt tác kiến trúc mang vẻ đẹp cổ kính của Kinh thành là kết tinh của tinh hoa kiến trúc Việt Nam. Không chỉ là chứng tích lịch sử mà còn là biểu tượng của một nền văn hóa lâu đời, là niềm tự hào của mỗi người con đất Việt.\r\nCùng ArWoh khám phá vẻ đẹp cổ kính của cố đô qua collection đầu tiên mang tên \"triều đại\". ",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            new()
            {
                Title = "Bình Minh Tại Đại Nội Huế",
                Description =
                    "Ánh bình minh đầu ngày phủ lên các kiến trúc cổ kính của Hoàng thành Huế, tạo nên sự tương phản mạnh mẽ giữa bóng tối và ánh sáng, làm nổi bật vẻ đẹp tinh xảo của kiến trúc hoàng gia.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FHue%2F%C4%90%E1%BA%A1i%20n%E1%BB%99i-06-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Huế", "Di sản", "Kiến trúc", "Hoàng cung", "Văn hóa" },
                Location = "Đại Nội Huế, Thừa Thiên Huế, Việt Nam",
                Price = 35000,
                FileName = "dai_noi_hue_binh_minh.png",
                StoryOfArt =
                    "\"Huế là một bài thơ, một bức tranh, một bản nhạc\". \r\nKinh Thành Huế - nơi hội tụ những giá trị văn hóa truyền thống, nơi những công trình kiến trúc cổ kính kể lại câu chuyện về một thời kỳ lịch sử hào hùng của dân tộc. Những tuyệt tác kiến trúc mang vẻ đẹp cổ kính của Kinh thành là kết tinh của tinh hoa kiến trúc Việt Nam. Không chỉ là chứng tích lịch sử mà còn là biểu tượng của một nền văn hóa lâu đời, là niềm tự hào của mỗi người con đất Việt.\r\nCùng ArWoh khám phá vẻ đẹp cổ kính của cố đô qua collection đầu tiên mang tên \"triều đại\". ",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },
            //6
            new()
            {
                Title = "Bình Minh Tại Đại Nội Huế",
                Description =
                    "Ánh bình minh đầu ngày phủ lên các kiến trúc cổ kính của Hoàng thành Huế, tạo nên sự tương phản mạnh mẽ giữa bóng tối và ánh sáng, làm nổi bật vẻ đẹp tinh xảo của kiến trúc hoàng gia.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FHue%2F%C4%90%E1%BA%A1i%20n%E1%BB%99i-07-min.png&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Huế", "Di sản", "Kiến trúc", "Hoàng cung", "Văn hóa" },
                Location = "Đại Nội Huế, Thừa Thiên Huế, Việt Nam",
                Price = 35000,
                FileName = "dai_noi_hue_binh_minh.png",
                StoryOfArt =
                    "\"Huế là một bài thơ, một bức tranh, một bản nhạc\". \r\nKinh Thành Huế - nơi hội tụ những giá trị văn hóa truyền thống, nơi những công trình kiến trúc cổ kính kể lại câu chuyện về một thời kỳ lịch sử hào hùng của dân tộc. Những tuyệt tác kiến trúc mang vẻ đẹp cổ kính của Kinh thành là kết tinh của tinh hoa kiến trúc Việt Nam. Không chỉ là chứng tích lịch sử mà còn là biểu tượng của một nền văn hóa lâu đời, là niềm tự hào của mỗi người con đất Việt.\r\nCùng ArWoh khám phá vẻ đẹp cổ kính của cố đô qua collection đầu tiên mang tên \"triều đại\". ",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            new()
            {
                Title = "Bình Minh Tại Đại Nội Huế",
                Description =
                    "Ánh bình minh đầu ngày phủ lên các kiến trúc cổ kính của Hoàng thành Huế, tạo nên sự tương phản mạnh mẽ giữa bóng tối và ánh sáng, làm nổi bật vẻ đẹp tinh xảo của kiến trúc hoàng gia.",
                Url =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FHue%2F%C4%90%E1%BA%A1i%20n%E1%BB%99i-08-min.jpg&version_id=null",
                Orientation = OrientationType.Portrait,
                Tags = new List<string> { "Huế", "Di sản", "Kiến trúc", "Hoàng cung", "Văn hóa" },
                Location = "Đại Nội Huế, Thừa Thiên Huế, Việt Nam",
                Price = 35000,
                FileName = "dai_noi_hue_binh_minh.png",
                StoryOfArt =
                    "\"Huế là một bài thơ, một bức tranh, một bản nhạc\". \r\nKinh Thành Huế - nơi hội tụ những giá trị văn hóa truyền thống, nơi những công trình kiến trúc cổ kính kể lại câu chuyện về một thời kỳ lịch sử hào hùng của dân tộc. Những tuyệt tác kiến trúc mang vẻ đẹp cổ kính của Kinh thành là kết tinh của tinh hoa kiến trúc Việt Nam. Không chỉ là chứng tích lịch sử mà còn là biểu tượng của một nền văn hóa lâu đời, là niềm tự hào của mỗi người con đất Việt.\r\nCùng ArWoh khám phá vẻ đẹp cổ kính của cố đô qua collection đầu tiên mang tên \"triều đại\". ",
                PhotographerId = photographers[random.Next(photographers.Count)]
            },

            #endregion
        };

        await _context.Images.AddRangeAsync(images);
        await _context.SaveChangesAsync();

        _logger.Success("User and image seeding completed successfully.");
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
                async () => await context.CartItems.ExecuteDeleteAsync(),
                async () => await context.AdminActions.ExecuteDeleteAsync(),
                async () => await context.Carts.ExecuteDeleteAsync(),
                async () => await context.Payments.ExecuteDeleteAsync(),
                async () => await context.Orders.ExecuteDeleteAsync(),
                async () => await context.OrderDetails.ExecuteDeleteAsync(),
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
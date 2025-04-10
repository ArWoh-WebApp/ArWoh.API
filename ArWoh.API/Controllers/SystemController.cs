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
                Username = "Tiến User",
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
                Username = "Dương Domic",
                Email = "a@gmail.com",
                PasswordHash = passwordHasher.HashPassword("a"),
                Role = UserRole.Photographer,
                Bio = "Ca sĩ nhưng thích chụp hình",
                ProfilePictureUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=user-avatars%2Fadad.jpg&version_id=null"
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
                    "<h2>Golden Waves: The Desert's Shifting Canvas</h2><br><br>1. Environmental Extremes. This photograph was taken in 48°C (118°F) heat, necessitating special thermal protection for both photographer and equipment.\n\n2. Transient Beauty. The specific dune formations shown will no longer exist in their captured form, as desert winds continually reshape the landscape.\n\n3. Color Science. The rich orange hues result from the high iron oxide content in the sand, amplified by the low-angle sunlight at golden hour.\n\n4. Scale Deception. While appearing modest in size, the largest dune in frame stands over 150 meters tall, demonstrating the difficulty of conveying true scale in desert environments.\n\n5. Silent Witness. These dunes have remained largely unchanged in process for over 4,000 years, though their exact shapes shift daily with the wind.",
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
                    "<h2>The Golden Corridor: Autumn's Transient Gallery</h2><br><br>1. Seasonal Peak. This image captures the precise 72-hour window when fall colors reached their maximum vibrancy before beginning to fade and fall.\n\n2. Biological Symphony. The varied colors represent different tree species' unique chemical responses to decreasing daylight - predominantly sugar maples (red), birch (yellow), and oak (russet).\n\n3. Historical Route. The path visible dates to the 18th century and was originally used for transporting maple sap from the forest to local sugar houses.\n\n4. Photographic Technique. To achieve the tunnel effect while maintaining focus throughout, I used focus stacking of seven separate exposures with incrementally different focal points.\n\n5. Climate Concern. Rising regional temperatures have delayed peak foliage season by nearly two weeks compared to historical records from the 1950s.",
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
                    "<h2>Edge of Worlds: Where Land Confronts Ocean</h2><br><br>1. Geological Testament. These cliffs contain visible sedimentary layers representing over 300 million years of Earth's history, readable like pages in a stone book.\n\n2. Weather Challenge. Capturing this image required enduring near-gale force winds that threatened both equipment stability and photographer safety.\n\n3. Timing Precision. The specific light angle occurs only twice yearly when the sunrise aligns perfectly with the cliff orientation during the spring equinox.\n\n4. Sound Landscape. The thunderous crash of waves against rock created vibrations powerful enough to be felt through the ground at the shooting location 70 meters above sea level.\n\n5. Conservation Significance. This coastline serves as critical nesting habitat for endangered seabird species, with restrictions on human access during breeding seasons.",
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
                    "<h2>Mirror of the Sky: Perfect Alpine Symmetry</h2><br><br>1. Glacial Origin. This lake formed approximately 12,000 years ago during the last ice age when retreating glaciers carved the basin and filled it with pristine meltwater.\n\n2. Rare Conditions. The perfect reflection captured required absolute stillness in both water and air - conditions that occur on fewer than 10 days annually in this high-altitude, typically windy environment.\n\n3. Water Chemistry. The distinctive turquoise color results from 'rock flour' - microscopic glacial sediment suspended in the water that reflects specific light wavelengths.\n\n4. Indigenous Significance. This location holds sacred status for local First Nations peoples, who have traditional stories describing the lake as a portal between worlds.\n\n5. Expedition Challenge. Reaching this remote shooting location required a three-day backcountry hike with all equipment carried by hand, as no vehicle or helicopter access exists.",
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
                    "<h2>Electric Rain: Urban Symphony After Dark</h2><br><br>1. Cultural Crossroads. This intersection marks the boundary between Seoul's traditional market district and its modern technology hub - a physical manifestation of the country's rapid transformation.\n\n2. Technical Approach. This image employs intentional lens distortion to enhance the surreal quality of the reflections, emphasizing the disorienting experience of urban spaces in adverse weather.\n\n3. Human Element. Though seemingly anonymous, several pedestrians granted permission to be included in the final image, understanding they would become part of an artistic representation of their city.\n\n4. Light Complexity. The scene contains over 200 distinct light sources in various colors and intensities, creating a challenging exposure situation requiring careful dynamic range management.\n\n5. Urban Evolution. This exact view no longer exists as several buildings visible have since been replaced with newer structures - the photograph now serves as historical documentation of a specific moment in the city's development.",
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
                    "<h2>Living Monuments: The Olive Guardians</h2><br><br>1. Botanical Elders. Several trees in this grove have been carbon-dated to be over 1,500 years old, making them living links to the Byzantine period when they were first planted.\n\n2. Agricultural Continuity. The same families have harvested olives from these trees for 27 generations, maintaining traditional farming techniques passed down through centuries.\n\n3. Environmental Adaptation. The twisted forms of the trunks represent the trees' response to prevailing winds and periodic drought - a physical record of environmental conditions spanning millennia.\n\n4. Seasonal Selection. The photograph was taken during the two-week period before harvest when the olives have reached full maturity but remain on the branches.\n\n5. Conservation Challenge. This ancient grove now faces threats from a bacterial disease spreading through the region, potentially endangering trees that have survived countless previous challenges throughout history.",
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
                    "<h2>Living Monuments: The Olive Guardians</h2><br><br>1. Botanical Elders. Several trees in this grove have been carbon-dated to be over 1,500 years old, making them living links to the Byzantine period when they were first planted.\n\n2. Agricultural Continuity. The same families have harvested olives from these trees for 27 generations, maintaining traditional farming techniques passed down through centuries.\n\n3. Environmental Adaptation. The twisted forms of the trunks represent the trees' response to prevailing winds and periodic drought - a physical record of environmental conditions spanning millennia.\n\n4. Seasonal Selection. The photograph was taken during the two-week period before harvest when the olives have reached full maturity but remain on the branches.\n\n5. Conservation Challenge. This ancient grove now faces threats from a bacterial disease spreading through the region, potentially endangering trees that have survived countless previous challenges throughout history.",
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
                    "<h2>Chuyển Mình Đô Thị: Nhịp Đập Mới Của Sài Gòn</h2><br><br>1. Dấu Mốc Lịch Sử. Tuyến Metro số 1 là dự án giao thông công cộng hiện đại đầu tiên của Thành phố Hồ Chí Minh, đánh dấu bước ngoặt quan trọng trong quá trình phát triển hạ tầng đô thị Việt Nam.\n\n2. Công Nghệ Tiên Tiến. Hệ thống tàu điện ngầm này sử dụng công nghệ tự động hóa cao, với khả năng vận hành an toàn ở tốc độ tối đa 110km/h, kết nối các khu vực trọng điểm của thành phố.\n\n3. Thách Thức Kỹ Thuật. Quá trình xây dựng đối mặt với nhiều khó khăn do đặc điểm địa chất phức tạp của khu vực, đòi hỏi kỹ thuật đào hầm tiên tiến và sự hợp tác quốc tế từ nhiều chuyên gia.\n\n4. Tác Động Xã Hội. Bức ảnh ghi lại khoảnh khắc chuyển giao giữa phương thức di chuyển truyền thống và hiện đại, phản ánh sự thay đổi trong lối sống và thói quen đi lại của người dân thành phố.\n\n5. Góc Nhìn Nghệ Thuật. Ánh sáng nhân tạo từ hệ thống chiếu sáng của ga tàu tạo ra một không gian màu sắc tương phản với bầu trời hoàng hôn bên ngoài, tượng trưng cho sự giao thoa giữa tự nhiên và công nghệ trong đô thị hiện đại.",
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
                    "<h2>Chuyển Mình Đô Thị: Nhịp Đập Mới Của Sài Gòn</h2><br><br>1. Dấu Mốc Lịch Sử. Tuyến Metro số 1 là dự án giao thông công cộng hiện đại đầu tiên của Thành phố Hồ Chí Minh, đánh dấu bước ngoặt quan trọng trong quá trình phát triển hạ tầng đô thị Việt Nam.\n\n2. Công Nghệ Tiên Tiến. Hệ thống tàu điện ngầm này sử dụng công nghệ tự động hóa cao, với khả năng vận hành an toàn ở tốc độ tối đa 110km/h, kết nối các khu vực trọng điểm của thành phố.\n\n3. Thách Thức Kỹ Thuật. Quá trình xây dựng đối mặt với nhiều khó khăn do đặc điểm địa chất phức tạp của khu vực, đòi hỏi kỹ thuật đào hầm tiên tiến và sự hợp tác quốc tế từ nhiều chuyên gia.\n\n4. Tác Động Xã Hội. Bức ảnh ghi lại khoảnh khắc chuyển giao giữa phương thức di chuyển truyền thống và hiện đại, phản ánh sự thay đổi trong lối sống và thói quen đi lại của người dân thành phố.\n\n5. Góc Nhìn Nghệ Thuật. Ánh sáng nhân tạo từ hệ thống chiếu sáng của ga tàu tạo ra một không gian màu sắc tương phản với bầu trời hoàng hôn bên ngoài, tượng trưng cho sự giao thoa giữa tự nhiên và công nghệ trong đô thị hiện đại.",
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
                    "<h2>Chuyển Mình Đô Thị: Nhịp Đập Mới Của Sài Gòn</h2><br><br>1. Dấu Mốc Lịch Sử. Tuyến Metro số 1 là dự án giao thông công cộng hiện đại đầu tiên của Thành phố Hồ Chí Minh, đánh dấu bước ngoặt quan trọng trong quá trình phát triển hạ tầng đô thị Việt Nam.\n\n2. Công Nghệ Tiên Tiến. Hệ thống tàu điện ngầm này sử dụng công nghệ tự động hóa cao, với khả năng vận hành an toàn ở tốc độ tối đa 110km/h, kết nối các khu vực trọng điểm của thành phố.\n\n3. Thách Thức Kỹ Thuật. Quá trình xây dựng đối mặt với nhiều khó khăn do đặc điểm địa chất phức tạp của khu vực, đòi hỏi kỹ thuật đào hầm tiên tiến và sự hợp tác quốc tế từ nhiều chuyên gia.\n\n4. Tác Động Xã Hội. Bức ảnh ghi lại khoảnh khắc chuyển giao giữa phương thức di chuyển truyền thống và hiện đại, phản ánh sự thay đổi trong lối sống và thói quen đi lại của người dân thành phố.\n\n5. Góc Nhìn Nghệ Thuật. Ánh sáng nhân tạo từ hệ thống chiếu sáng của ga tàu tạo ra một không gian màu sắc tương phản với bầu trời hoàng hôn bên ngoài, tượng trưng cho sự giao thoa giữa tự nhiên và công nghệ trong đô thị hiện đại.",
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
                    "<h2>Chuyển Mình Đô Thị: Nhịp Đập Mới Của Sài Gòn</h2><br><br>1. Dấu Mốc Lịch Sử. Tuyến Metro số 1 là dự án giao thông công cộng hiện đại đầu tiên của Thành phố Hồ Chí Minh, đánh dấu bước ngoặt quan trọng trong quá trình phát triển hạ tầng đô thị Việt Nam.\n\n2. Công Nghệ Tiên Tiến. Hệ thống tàu điện ngầm này sử dụng công nghệ tự động hóa cao, với khả năng vận hành an toàn ở tốc độ tối đa 110km/h, kết nối các khu vực trọng điểm của thành phố.\n\n3. Thách Thức Kỹ Thuật. Quá trình xây dựng đối mặt với nhiều khó khăn do đặc điểm địa chất phức tạp của khu vực, đòi hỏi kỹ thuật đào hầm tiên tiến và sự hợp tác quốc tế từ nhiều chuyên gia.\n\n4. Tác Động Xã Hội. Bức ảnh ghi lại khoảnh khắc chuyển giao giữa phương thức di chuyển truyền thống và hiện đại, phản ánh sự thay đổi trong lối sống và thói quen đi lại của người dân thành phố.\n\n5. Góc Nhìn Nghệ Thuật. Ánh sáng nhân tạo từ hệ thống chiếu sáng của ga tàu tạo ra một không gian màu sắc tương phản với bầu trời hoàng hôn bên ngoài, tượng trưng cho sự giao thoa giữa tự nhiên và công nghệ trong đô thị hiện đại.",
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
                    "<h2>Chuyển Mình Đô Thị: Nhịp Đập Mới Của Sài Gòn</h2><br><br>1. Dấu Mốc Lịch Sử. Tuyến Metro số 1 là dự án giao thông công cộng hiện đại đầu tiên của Thành phố Hồ Chí Minh, đánh dấu bước ngoặt quan trọng trong quá trình phát triển hạ tầng đô thị Việt Nam.\n\n2. Công Nghệ Tiên Tiến. Hệ thống tàu điện ngầm này sử dụng công nghệ tự động hóa cao, với khả năng vận hành an toàn ở tốc độ tối đa 110km/h, kết nối các khu vực trọng điểm của thành phố.\n\n3. Thách Thức Kỹ Thuật. Quá trình xây dựng đối mặt với nhiều khó khăn do đặc điểm địa chất phức tạp của khu vực, đòi hỏi kỹ thuật đào hầm tiên tiến và sự hợp tác quốc tế từ nhiều chuyên gia.\n\n4. Tác Động Xã Hội. Bức ảnh ghi lại khoảnh khắc chuyển giao giữa phương thức di chuyển truyền thống và hiện đại, phản ánh sự thay đổi trong lối sống và thói quen đi lại của người dân thành phố.\n\n5. Góc Nhìn Nghệ Thuật. Ánh sáng nhân tạo từ hệ thống chiếu sáng của ga tàu tạo ra một không gian màu sắc tương phản với bầu trời hoàng hôn bên ngoài, tượng trưng cho sự giao thoa giữa tự nhiên và công nghệ trong đô thị hiện đại.",
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
                    "<h2>Chuyển Mình Đô Thị: Nhịp Đập Mới Của Sài Gòn</h2><br><br>1. Dấu Mốc Lịch Sử. Tuyến Metro số 1 là dự án giao thông công cộng hiện đại đầu tiên của Thành phố Hồ Chí Minh, đánh dấu bước ngoặt quan trọng trong quá trình phát triển hạ tầng đô thị Việt Nam.\n\n2. Công Nghệ Tiên Tiến. Hệ thống tàu điện ngầm này sử dụng công nghệ tự động hóa cao, với khả năng vận hành an toàn ở tốc độ tối đa 110km/h, kết nối các khu vực trọng điểm của thành phố.\n\n3. Thách Thức Kỹ Thuật. Quá trình xây dựng đối mặt với nhiều khó khăn do đặc điểm địa chất phức tạp của khu vực, đòi hỏi kỹ thuật đào hầm tiên tiến và sự hợp tác quốc tế từ nhiều chuyên gia.\n\n4. Tác Động Xã Hội. Bức ảnh ghi lại khoảnh khắc chuyển giao giữa phương thức di chuyển truyền thống và hiện đại, phản ánh sự thay đổi trong lối sống và thói quen đi lại của người dân thành phố.\n\n5. Góc Nhìn Nghệ Thuật. Ánh sáng nhân tạo từ hệ thống chiếu sáng của ga tàu tạo ra một không gian màu sắc tương phản với bầu trời hoàng hôn bên ngoài, tượng trưng cho sự giao thoa giữa tự nhiên và công nghệ trong đô thị hiện đại.",
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
                    "<h2>Chuyển Mình Đô Thị: Nhịp Đập Mới Của Sài Gòn</h2><br><br>1. Dấu Mốc Lịch Sử. Tuyến Metro số 1 là dự án giao thông công cộng hiện đại đầu tiên của Thành phố Hồ Chí Minh, đánh dấu bước ngoặt quan trọng trong quá trình phát triển hạ tầng đô thị Việt Nam.\n\n2. Công Nghệ Tiên Tiến. Hệ thống tàu điện ngầm này sử dụng công nghệ tự động hóa cao, với khả năng vận hành an toàn ở tốc độ tối đa 110km/h, kết nối các khu vực trọng điểm của thành phố.\n\n3. Thách Thức Kỹ Thuật. Quá trình xây dựng đối mặt với nhiều khó khăn do đặc điểm địa chất phức tạp của khu vực, đòi hỏi kỹ thuật đào hầm tiên tiến và sự hợp tác quốc tế từ nhiều chuyên gia.\n\n4. Tác Động Xã Hội. Bức ảnh ghi lại khoảnh khắc chuyển giao giữa phương thức di chuyển truyền thống và hiện đại, phản ánh sự thay đổi trong lối sống và thói quen đi lại của người dân thành phố.\n\n5. Góc Nhìn Nghệ Thuật. Ánh sáng nhân tạo từ hệ thống chiếu sáng của ga tàu tạo ra một không gian màu sắc tương phản với bầu trời hoàng hôn bên ngoài, tượng trưng cho sự giao thoa giữa tự nhiên và công nghệ trong đô thị hiện đại.",
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
                    "<h2>Chuyển Mình Đô Thị: Nhịp Đập Mới Của Sài Gòn</h2><br><br>1. Dấu Mốc Lịch Sử. Tuyến Metro số 1 là dự án giao thông công cộng hiện đại đầu tiên của Thành phố Hồ Chí Minh, đánh dấu bước ngoặt quan trọng trong quá trình phát triển hạ tầng đô thị Việt Nam.\n\n2. Công Nghệ Tiên Tiến. Hệ thống tàu điện ngầm này sử dụng công nghệ tự động hóa cao, với khả năng vận hành an toàn ở tốc độ tối đa 110km/h, kết nối các khu vực trọng điểm của thành phố.\n\n3. Thách Thức Kỹ Thuật. Quá trình xây dựng đối mặt với nhiều khó khăn do đặc điểm địa chất phức tạp của khu vực, đòi hỏi kỹ thuật đào hầm tiên tiến và sự hợp tác quốc tế từ nhiều chuyên gia.\n\n4. Tác Động Xã Hội. Bức ảnh ghi lại khoảnh khắc chuyển giao giữa phương thức di chuyển truyền thống và hiện đại, phản ánh sự thay đổi trong lối sống và thói quen đi lại của người dân thành phố.\n\n5. Góc Nhìn Nghệ Thuật. Ánh sáng nhân tạo từ hệ thống chiếu sáng của ga tàu tạo ra một không gian màu sắc tương phản với bầu trời hoàng hôn bên ngoài, tượng trưng cho sự giao thoa giữa tự nhiên và công nghệ trong đô thị hiện đại.",
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
                    "<h2>Dấu Ấn Hoàng Cung: Di Sản Văn Hóa Việt Nam</h2><br><br>1. Lịch Sử Trường Tồn. Đại Nội Huế được xây dựng từ năm 1805 dưới triều Gia Long, là trung tâm chính trị và văn hóa của triều đại nhà Nguyễn trong suốt 143 năm (1802-1945).\n\n2. Kiến Trúc Đặc Biệt. Công trình này là sự kết hợp hoàn hảo giữa nghệ thuật phương Đông truyền thống và các nguyên lý phong thủy, thể hiện qua việc bố trí các tòa nhà theo trục Bắc-Nam và được bao quanh bởi hệ thống sông Hương.\n\n3. Thách Thức Bảo Tồn. Bức ảnh này ghi lại một trong những góc còn nguyên vẹn của di sản, sau khi phần lớn công trình đã bị tàn phá qua hai cuộc chiến tranh và nhiều thập kỷ bị bỏ hoang.\n\n4. Giá Trị Biểu Tượng. Màu vàng hoàng gia trên các chi tiết kiến trúc là màu dành riêng cho hoàng tộc, tượng trưng cho quyền lực tối cao và sự thịnh vượng của triều đại.\n\n5. Góc Nhìn Nghệ Thuật. Bức ảnh được chụp vào thời điểm bình minh, khi ánh sáng đầu ngày tạo ra sự tương phản mạnh mẽ giữa bóng và sáng, làm nổi bật các đường nét kiến trúc tinh xảo - một khoảnh khắc hiếm có khi du khách chưa đến tham quan.",
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
                    "<h2>Dấu Ấn Hoàng Cung: Di Sản Văn Hóa Việt Nam</h2><br><br>1. Lịch Sử Trường Tồn. Đại Nội Huế được xây dựng từ năm 1805 dưới triều Gia Long, là trung tâm chính trị và văn hóa của triều đại nhà Nguyễn trong suốt 143 năm (1802-1945).\n\n2. Kiến Trúc Đặc Biệt. Công trình này là sự kết hợp hoàn hảo giữa nghệ thuật phương Đông truyền thống và các nguyên lý phong thủy, thể hiện qua việc bố trí các tòa nhà theo trục Bắc-Nam và được bao quanh bởi hệ thống sông Hương.\n\n3. Thách Thức Bảo Tồn. Bức ảnh này ghi lại một trong những góc còn nguyên vẹn của di sản, sau khi phần lớn công trình đã bị tàn phá qua hai cuộc chiến tranh và nhiều thập kỷ bị bỏ hoang.\n\n4. Giá Trị Biểu Tượng. Màu vàng hoàng gia trên các chi tiết kiến trúc là màu dành riêng cho hoàng tộc, tượng trưng cho quyền lực tối cao và sự thịnh vượng của triều đại.\n\n5. Góc Nhìn Nghệ Thuật. Bức ảnh được chụp vào thời điểm bình minh, khi ánh sáng đầu ngày tạo ra sự tương phản mạnh mẽ giữa bóng và sáng, làm nổi bật các đường nét kiến trúc tinh xảo - một khoảnh khắc hiếm có khi du khách chưa đến tham quan.",
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
                    "<h2>Dấu Ấn Hoàng Cung: Di Sản Văn Hóa Việt Nam</h2><br><br>1. Lịch Sử Trường Tồn. Đại Nội Huế được xây dựng từ năm 1805 dưới triều Gia Long, là trung tâm chính trị và văn hóa của triều đại nhà Nguyễn trong suốt 143 năm (1802-1945).\n\n2. Kiến Trúc Đặc Biệt. Công trình này là sự kết hợp hoàn hảo giữa nghệ thuật phương Đông truyền thống và các nguyên lý phong thủy, thể hiện qua việc bố trí các tòa nhà theo trục Bắc-Nam và được bao quanh bởi hệ thống sông Hương.\n\n3. Thách Thức Bảo Tồn. Bức ảnh này ghi lại một trong những góc còn nguyên vẹn của di sản, sau khi phần lớn công trình đã bị tàn phá qua hai cuộc chiến tranh và nhiều thập kỷ bị bỏ hoang.\n\n4. Giá Trị Biểu Tượng. Màu vàng hoàng gia trên các chi tiết kiến trúc là màu dành riêng cho hoàng tộc, tượng trưng cho quyền lực tối cao và sự thịnh vượng của triều đại.\n\n5. Góc Nhìn Nghệ Thuật. Bức ảnh được chụp vào thời điểm bình minh, khi ánh sáng đầu ngày tạo ra sự tương phản mạnh mẽ giữa bóng và sáng, làm nổi bật các đường nét kiến trúc tinh xảo - một khoảnh khắc hiếm có khi du khách chưa đến tham quan.",
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
                    "<h2>Dấu Ấn Hoàng Cung: Di Sản Văn Hóa Việt Nam</h2><br><br>1. Lịch Sử Trường Tồn. Đại Nội Huế được xây dựng từ năm 1805 dưới triều Gia Long, là trung tâm chính trị và văn hóa của triều đại nhà Nguyễn trong suốt 143 năm (1802-1945).\n\n2. Kiến Trúc Đặc Biệt. Công trình này là sự kết hợp hoàn hảo giữa nghệ thuật phương Đông truyền thống và các nguyên lý phong thủy, thể hiện qua việc bố trí các tòa nhà theo trục Bắc-Nam và được bao quanh bởi hệ thống sông Hương.\n\n3. Thách Thức Bảo Tồn. Bức ảnh này ghi lại một trong những góc còn nguyên vẹn của di sản, sau khi phần lớn công trình đã bị tàn phá qua hai cuộc chiến tranh và nhiều thập kỷ bị bỏ hoang.\n\n4. Giá Trị Biểu Tượng. Màu vàng hoàng gia trên các chi tiết kiến trúc là màu dành riêng cho hoàng tộc, tượng trưng cho quyền lực tối cao và sự thịnh vượng của triều đại.\n\n5. Góc Nhìn Nghệ Thuật. Bức ảnh được chụp vào thời điểm bình minh, khi ánh sáng đầu ngày tạo ra sự tương phản mạnh mẽ giữa bóng và sáng, làm nổi bật các đường nét kiến trúc tinh xảo - một khoảnh khắc hiếm có khi du khách chưa đến tham quan.",
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
                    "<h2>Dấu Ấn Hoàng Cung: Di Sản Văn Hóa Việt Nam</h2><br><br>1. Lịch Sử Trường Tồn. Đại Nội Huế được xây dựng từ năm 1805 dưới triều Gia Long, là trung tâm chính trị và văn hóa của triều đại nhà Nguyễn trong suốt 143 năm (1802-1945).\n\n2. Kiến Trúc Đặc Biệt. Công trình này là sự kết hợp hoàn hảo giữa nghệ thuật phương Đông truyền thống và các nguyên lý phong thủy, thể hiện qua việc bố trí các tòa nhà theo trục Bắc-Nam và được bao quanh bởi hệ thống sông Hương.\n\n3. Thách Thức Bảo Tồn. Bức ảnh này ghi lại một trong những góc còn nguyên vẹn của di sản, sau khi phần lớn công trình đã bị tàn phá qua hai cuộc chiến tranh và nhiều thập kỷ bị bỏ hoang.\n\n4. Giá Trị Biểu Tượng. Màu vàng hoàng gia trên các chi tiết kiến trúc là màu dành riêng cho hoàng tộc, tượng trưng cho quyền lực tối cao và sự thịnh vượng của triều đại.\n\n5. Góc Nhìn Nghệ Thuật. Bức ảnh được chụp vào thời điểm bình minh, khi ánh sáng đầu ngày tạo ra sự tương phản mạnh mẽ giữa bóng và sáng, làm nổi bật các đường nét kiến trúc tinh xảo - một khoảnh khắc hiếm có khi du khách chưa đến tham quan.",
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
                    "<h2>Dấu Ấn Hoàng Cung: Di Sản Văn Hóa Việt Nam</h2><br><br>1. Lịch Sử Trường Tồn. Đại Nội Huế được xây dựng từ năm 1805 dưới triều Gia Long, là trung tâm chính trị và văn hóa của triều đại nhà Nguyễn trong suốt 143 năm (1802-1945).\n\n2. Kiến Trúc Đặc Biệt. Công trình này là sự kết hợp hoàn hảo giữa nghệ thuật phương Đông truyền thống và các nguyên lý phong thủy, thể hiện qua việc bố trí các tòa nhà theo trục Bắc-Nam và được bao quanh bởi hệ thống sông Hương.\n\n3. Thách Thức Bảo Tồn. Bức ảnh này ghi lại một trong những góc còn nguyên vẹn của di sản, sau khi phần lớn công trình đã bị tàn phá qua hai cuộc chiến tranh và nhiều thập kỷ bị bỏ hoang.\n\n4. Giá Trị Biểu Tượng. Màu vàng hoàng gia trên các chi tiết kiến trúc là màu dành riêng cho hoàng tộc, tượng trưng cho quyền lực tối cao và sự thịnh vượng của triều đại.\n\n5. Góc Nhìn Nghệ Thuật. Bức ảnh được chụp vào thời điểm bình minh, khi ánh sáng đầu ngày tạo ra sự tương phản mạnh mẽ giữa bóng và sáng, làm nổi bật các đường nét kiến trúc tinh xảo - một khoảnh khắc hiếm có khi du khách chưa đến tham quan.",
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
                    "<h2>Dấu Ấn Hoàng Cung: Di Sản Văn Hóa Việt Nam</h2><br><br>1. Lịch Sử Trường Tồn. Đại Nội Huế được xây dựng từ năm 1805 dưới triều Gia Long, là trung tâm chính trị và văn hóa của triều đại nhà Nguyễn trong suốt 143 năm (1802-1945).\n\n2. Kiến Trúc Đặc Biệt. Công trình này là sự kết hợp hoàn hảo giữa nghệ thuật phương Đông truyền thống và các nguyên lý phong thủy, thể hiện qua việc bố trí các tòa nhà theo trục Bắc-Nam và được bao quanh bởi hệ thống sông Hương.\n\n3. Thách Thức Bảo Tồn. Bức ảnh này ghi lại một trong những góc còn nguyên vẹn của di sản, sau khi phần lớn công trình đã bị tàn phá qua hai cuộc chiến tranh và nhiều thập kỷ bị bỏ hoang.\n\n4. Giá Trị Biểu Tượng. Màu vàng hoàng gia trên các chi tiết kiến trúc là màu dành riêng cho hoàng tộc, tượng trưng cho quyền lực tối cao và sự thịnh vượng của triều đại.\n\n5. Góc Nhìn Nghệ Thuật. Bức ảnh được chụp vào thời điểm bình minh, khi ánh sáng đầu ngày tạo ra sự tương phản mạnh mẽ giữa bóng và sáng, làm nổi bật các đường nét kiến trúc tinh xảo - một khoảnh khắc hiếm có khi du khách chưa đến tham quan.",
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
                    "<h2>Dấu Ấn Hoàng Cung: Di Sản Văn Hóa Việt Nam</h2><br><br>1. Lịch Sử Trường Tồn. Đại Nội Huế được xây dựng từ năm 1805 dưới triều Gia Long, là trung tâm chính trị và văn hóa của triều đại nhà Nguyễn trong suốt 143 năm (1802-1945).\n\n2. Kiến Trúc Đặc Biệt. Công trình này là sự kết hợp hoàn hảo giữa nghệ thuật phương Đông truyền thống và các nguyên lý phong thủy, thể hiện qua việc bố trí các tòa nhà theo trục Bắc-Nam và được bao quanh bởi hệ thống sông Hương.\n\n3. Thách Thức Bảo Tồn. Bức ảnh này ghi lại một trong những góc còn nguyên vẹn của di sản, sau khi phần lớn công trình đã bị tàn phá qua hai cuộc chiến tranh và nhiều thập kỷ bị bỏ hoang.\n\n4. Giá Trị Biểu Tượng. Màu vàng hoàng gia trên các chi tiết kiến trúc là màu dành riêng cho hoàng tộc, tượng trưng cho quyền lực tối cao và sự thịnh vượng của triều đại.\n\n5. Góc Nhìn Nghệ Thuật. Bức ảnh được chụp vào thời điểm bình minh, khi ánh sáng đầu ngày tạo ra sự tương phản mạnh mẽ giữa bóng và sáng, làm nổi bật các đường nét kiến trúc tinh xảo - một khoảnh khắc hiếm có khi du khách chưa đến tham quan.",
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
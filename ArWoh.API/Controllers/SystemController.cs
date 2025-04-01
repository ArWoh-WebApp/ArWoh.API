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
                new()
    {
        Title = "Mountain Stream at Dawn",
        Description = "Captured during the early hours of dawn...",
        Url = "https://images.unsplash.com/photo-1548679847-1d4ff48016c7",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Water Stream", "Brook", "Natural Water", "Landscape", "Mountains" },
        Location = "Rocky Mountains, Colorado",
        Price = 2000,
        FileName = "mountain_stream_dawn.jpg",
        StoryOfArt = "## **The Journey of Light and Water**\n\n1. Dawn's First Light. The photograph captures that magical moment when the first rays of sunlight pierce through the mountain mist, creating golden reflections on the flowing stream.\n\n2. Technical Challenges. Shooting in low-light conditions required a delicate balance between exposure time and aperture to maintain the crispness of the moving water while preserving the warm glow of dawn.\n\n3. Personal Connection. This location holds special significance as it marks the beginning of my journey as a landscape photographer, teaching me patience and the rewards of rising before the sun.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "City Skyline at Dusk",
        Description = "As daylight fades, the city transforms...",
        Url = "https://images.unsplash.com/photo-1502635994848-2eb3b4a38201",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "Skyline", "City", "Nightlife", "Urban" },
        Location = "New York City, USA",
        Price = 30000,
        FileName = "city_skyline_dusk.jpg",
        StoryOfArt = "## **Urban Metamorphosis: A City's Twilight Tale**\n\n1. The Transition Hour. This image was taken during the 'blue hour' - that brief window after sunset when artificial lights begin to glow but the sky still retains deep blue tones rather than complete darkness.\n\n2. Architectural Symphony. The composition deliberately juxtaposes historical buildings with modern skyscrapers, creating a visual narrative of the city's evolution through time.\n\n3. Human Element. Though no people are visible, the thousands of illuminated windows represent countless individual stories unfolding simultaneously across the urban landscape.\n\n4. Technical Approach. A long exposure of 15 seconds allowed me to capture the light trails of moving vehicles, adding dynamism to the otherwise static cityscape.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Snowy Mountain Peaks",
        Description = "Towering peaks dusted with pristine snow...",
        Url = "https://images.unsplash.com/photo-1558089551-95d707e6c13c",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "Mountains", "Snow", "Alps", "Winter" },
        Location = "Swiss Alps, Switzerland",
        Price = 20000,
        FileName = "snowy_mountain_peaks.jpg",
        StoryOfArt = "## **Silence in White: Alpine Majesty**\n\n1. Nature's Monument. These ancient mountains, formed over millions of years, stand as silent sentinels covered in their winter blanket, untouched and pristine.\n\n2. The Climb. Reaching this vantage point required a three-day trek through increasingly harsh conditions, with temperatures dropping to -15°C on the final morning.\n\n3. Light's Perfection. The image was captured during the phenomenon known as 'alpenglow,' when the peaks are illuminated with a reddish glow while the valleys remain in shadow.\n\n4. Scale and Perspective. The vastness of these mountains reminds us of our place in the natural world - temporary visitors in an ancient landscape that will outlast generations.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Vibrant Flower Garden",
        Description = "Lush, blooming flowers create a lively mosaic...",
        Url = "https://images.unsplash.com/photo-1428908728789-d2de25dbd4e2",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Flowers", "Garden", "Colorful", "Nature" },
        Location = "Provence, France",
        Price = 23000,
        FileName = "vibrant_flower_garden.jpg",
        StoryOfArt = "## **Nature's Color Palette: The Gardener's Canvas**\n\n1. Historical Context. This garden has been maintained by five generations of the same family, with planting techniques passed down through oral tradition since the early 19th century.\n\n2. Biodiversity Focus. Over 45 different species of flowering plants are visible in this single frame, each selected not only for visual impact but also to support local pollinator populations.\n\n3. Compositional Technique. The seeming randomness of the garden belies the careful color theory application - complementary colors are strategically positioned to enhance visual vibrancy.\n\n4. Seasonal Storytelling. The image captures the garden at its peak bloom during the three-week window in late spring when all varieties reach their full expression simultaneously.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Rainy City Street",
        Description = "Rain-soaked streets reflect neon signs and city lights, creating an atmospheric urban landscape. The scene captures the reflective mood and charm of a rainy evening.",
        Url = "https://images.unsplash.com/photo-1503348379917-758650634df4?q=80&w=1887&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "Rain", "City", "Urban", "Night" },
        Location = "Tokyo, Japan",
        Price = 50000,
        FileName = "rainy_city_street.jpg",
        StoryOfArt = "Liquid Light: Tokyo After Rainfall.\n\n1. Moment of Serendipity. This image was captured during an unexpected thunderstorm when most pedestrians had sought shelter, leaving the normally bustling street momentarily empty.\n\n2. Cultural Reflection. The neon signs displaying both Japanese characters and English text represent the unique cultural fusion that defines modern Tokyo.\n\n3. Technical Patience. I waited under an awning for nearly two hours for the precise moment when the rain intensity created perfect reflections without obscuring the distant details.\n\n4. Emotional Resonance. The scene evokes the distinctive feeling of being alone in a crowd - a common experience in mega-cities where millions live in close proximity yet often in emotional isolation.\n\n5. Visual Metaphor. The reflected lights create a parallel world beneath the surface, suggesting the duality between Tokyo's polished exterior and its complex undercurrents.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Sunset Over the Lake",
        Description = "The sky bursts into a palette of warm colors as the sun sets, casting a gentle glow over a tranquil lake. The peaceful scene invites relaxation and introspection.",
        Url = "https://images.unsplash.com/photo-1514975440715-7b6852af4ee7?q=80&w=1740&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Sunset", "Lake", "Reflection", "Nature" },
        Location = "Lake Tahoe, USA",
        Price = 24000,
        FileName = "sunset_over_the_lake.jpg",
        StoryOfArt = "Day's End: The Lake's Sunset Symphony.\n\n1. Perfect Stillness. After three days of strong winds, the lake surface became completely calm on this evening, creating a mirror-like reflection that doubles the visual impact of the sunset.\n\n2. Color Science. The exceptional pink and purple hues resulted from distant forest fire particles in the atmosphere, filtering the light wavelengths in a way rarely seen in this region.\n\n3. Sound Dimension. Though impossible to convey in a photograph, this moment was accompanied by the evening chorus of loons calling across the water - adding an auditory dimension to the visual experience.\n\n4. Conservation Message. This pristine view exists today thanks to conservation efforts that prevented commercial development along this shoreline in the 1970s, preserving the natural viewshed for future generations.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Starry Night Sky",
        Description = "Under the cloak of night, countless stars glitter against the deep blue backdrop. The image captures the infinite expanse of the universe, inspiring wonder and curiosity.",
        Url = "https://images.unsplash.com/photo-1595246965570-9684145def50?q=80&w=1887&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "Night", "Stars", "Astrophotography", "Universe" },
        Location = "Atacama Desert, Chile",
        Price = 40000,
        FileName = "starry_night_sky.jpg",
        StoryOfArt = "Cosmic Canvas: Windows to Infinity.\n\n1. Light Years Away. What appears as simple points of light represents ancient photons that have traveled for centuries or millennia before reaching my camera sensor.\n\n2. Technical Excellence. This image is a composite of 85 separate 30-second exposures, meticulously aligned and stacked to reveal celestial details invisible to the naked eye.\n\n3. Geographic Advantage. The Atacama Desert's extreme altitude, dry air, and distance from light pollution make it one of only three locations on Earth where such clarity of the night sky is possible.\n\n4. Scientific Value. Several previously undocumented stellar phenomena were discovered in this image upon detailed analysis, leading to collaboration with astronomers from the European Southern Observatory.\n\n5. Human Context. The small silhouette of a traditional observatory dome in the lower frame provides scale, contrasting human endeavors with the incomprehensible vastness above.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Countryside Road",
        Description = "The rustic charm of the countryside is on full display with a solitary road stretching between rolling fields and quaint farmhouses. The image evokes a sense of freedom and simplicity.",
        Url = "https://images.unsplash.com/photo-1499796683658-b659bc751db1?q=80&w=1974&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Countryside", "Road", "Farms", "Landscape" },
        Location = "Tuscany, Italy",
        Price = 40000,
        FileName = "countryside_road.jpg",
        StoryOfArt = "The Winding Path: Tuscany's Timeless Journey.\n\n1. Historical Pathway. This road follows the route of an ancient Roman trade path that has connected hillside villages for over two millennia.\n\n2. Agricultural Heritage. The distinctive pattern of cypress trees lining the road was established in the 15th century as wind protection for the valuable crops growing in adjacent fields.\n\n3. Light Study. The late afternoon sun position creates alternating patterns of light and shadow that emphasize the undulating topography of the landscape.\n\n4. Preservation Challenges. This iconic view now faces threats from modernization and climate change, with traditional farming practices giving way to more industrialized methods.\n\n5. Personal Discovery. I found this specific vantage point by getting completely lost while cycling through back roads, a reminder that sometimes the best compositions appear when we deviate from planned routes.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Ancient Castle Ruins",
        Description = "Weathered stone walls and crumbling towers speak of bygone eras. The dramatic sky adds to the eerie charm of these forgotten castle ruins.",
        Url = "https://images.unsplash.com/photo-1482938289607-e9573fc25ebb?q=80&w=1887&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "Castle", "Ruins", "History", "Mystery" },
        Location = "Scotland, UK",
        Price = 40000,
        FileName = "ancient_castle_ruins.jpg",
        StoryOfArt = "Echoes in Stone: A Fortress Remembers.\n\n1. Archaeological Context. These ruins date to the 13th century and were once a strategic stronghold controlling passage through the valley below during the Scottish Wars of Independence.\n\n2. Dramatic Timing. The threatening storm clouds were approaching rapidly when this image was taken, with the first raindrops beginning to fall as I captured the final frame in the sequence.\n\n3. Compositional Intent. I deliberately positioned myself to frame the central tower against the brightest part of the sky, creating a silhouette effect that emphasizes the castle's imposing presence even in its deteriorated state.\n\n4. Hidden Details. Close examination reveals medieval mason marks still visible on several stones - the signatures of craftsmen who died nearly 800 years ago.\n\n5. Conservation Status. Though appearing untouched by modern intervention, these ruins underwent careful stabilization work in the 1990s to prevent complete collapse while maintaining their authentic weathered appearance.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Lush Tropical Forest",
        Description = "Dense foliage and a riot of green hues define this tropical forest. The light filters through the canopy, creating a magical interplay of shadow and brightness.",
        Url = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?q=80&w=1744&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Tropical", "Forest", "Green", "Nature" },
        Location = "Costa Rica",
        Price = 40000,
        FileName = "lush_tropical_forest.jpg",
        StoryOfArt = "Green Cathedral: The Rainforest's Secret World.\n\n1. Biodiversity Hotspot. The small area captured in this single frame contains over 200 distinct plant species, representing one of the highest concentrations of botanical diversity on the planet.\n\n2. Light Phenomenon. The sunbeams visible in the image result from a rare alignment that occurs for only 20 minutes each day when the sun reaches a specific angle relative to the canopy gaps.\n\n3. Ecological Narrative. The varying shades of green represent different canopy layers, each creating its own microhabitat for specialized flora and fauna.\n\n4. Sensory Experience. The photograph cannot convey the accompanying sounds and smells - the constant dripping of condensation, the calls of unseen birds, and the rich earthy aroma of decomposition and growth.\n\n5. Conservation Status. This particular forest section stands in a protected biological corridor that connects larger preserves, allowing wildlife migration patterns to continue despite surrounding development pressure.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Calm River Bend",
        Description = "A meandering river reflects the soft hues of a fading day. The scene exudes tranquility and invites viewers to savor the simplicity of nature's flow.",
        Url = "https://images.unsplash.com/photo-1483959651481-dc75b89291f1?q=80&w=1849&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "River", "Calm", "Landscape", "Nature" },
        Location = "Loire Valley, France",
        Price = 40000,
        FileName = "calm_river_bend.jpg",
        StoryOfArt = "Water's Patient Journey: The River's Narrative.\n\n1. Geological Timekeeper. This gentle curve has been shaped over millennia, with the river slowly carving its path through soft limestone, revealing layers of Earth's history in the exposed banks.\n\n2. Historical Significance. For centuries, this bend served as a critical navigation point for trade vessels, with records of its use dating back to Roman times.\n\n3. Ecological Function. The slower water flow at this bend creates a natural deposition zone where nutrients collect, supporting the lush vegetation visible along the shoreline.\n\n4. Atmospheric Conditions. The unusual stillness of the water resulted from a rare weather phenomenon where air temperature and water temperature reached perfect equilibrium, eliminating all surface disturbance.\n\n5. Artistic Heritage. This same view has inspired painters since the Impressionist movement, with several notable works by Monet featuring this identical bend from slightly different angles.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Modern Architecture",
        Description = "This image highlights the intersection of art and engineering. Clean geometric forms and expansive glass surfaces define a modern structure set against a clear sky.",
        Url = "https://images.unsplash.com/photo-1518599904199-0ca897819ddb?q=80&w=1734&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Architecture", "Modern", "Design", "Urban" },
        Location = "Dubai, UAE",
        Price = 40000,
        FileName = "modern_architecture.jpg",
        StoryOfArt = "Geometry in Glass: The Future Manifested.\n\n1. Architectural Innovation. This structure pioneered a revolutionary tensile support system that allows the seemingly impossible cantilevers to extend without visible external support.\n\n2. Environmental Design. Despite its location in one of Earth's hottest climates, the building achieves remarkable energy efficiency through smart glass technology and passive cooling systems integrated into its geometric design.\n\n3. Cultural Context. The angular forms reference traditional regional architectural motifs, modernized through contemporary materials and engineering.\n\n4. Photographic Challenge. Capturing this image required precise timing to avoid lens flare from the harsh desert sun while maintaining the clarity of both the reflective surfaces and the sky.\n\n5. Design Philosophy. The architect described this building as 'crystallized music' - an attempt to give physical form to mathematical harmonies through proportion and rhythm in the structural elements.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Misty Morning in the Valley",
        Description = "Early morning mist envelops the valley, softening the landscape and lending an ethereal quality to the rolling hills and scattered trees. The quiet ambiance is both soothing and mysterious.",
        Url = "https://images.unsplash.com/photo-1582562478517-6f88924e668d?q=80&w=1827&auto=format&fit=crop&ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "Mist", "Morning", "Valley", "Nature" },
        Location = "Yosemite Valley, USA",
        Price = 40000,
        FileName = "misty_morning_valley.jpg",
        StoryOfArt = "Veiled Landscape: Dawn's Ethereal Moment.\n\n1. Meteorological Marvel. The mist formation captured here results from a temperature inversion where cold air becomes trapped beneath warmer air, a phenomenon that occurs in this valley fewer than 20 days each year.\n\n2. Historical Perspective. This view closely resembles what the first European explorers would have witnessed upon discovering the valley in the 19th century, with minimal visible human intervention in the landscape.\n\n3. Compositional Patience. I visited this overlook for seven consecutive mornings before atmospheric conditions aligned perfectly with the ideal light angle.\n\n4. Acoustic Dimension. The mist created an unusual acoustic environment where distant sounds (birds, falling water) seemed amplified and closer than their actual locations.\n\n5. Ephemeral Nature. Within 30 minutes of taking this photograph, the mist had completely dissipated, revealing an entirely different landscape beneath - a reminder of nature's constant state of transition.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },

    // SEED ẢNH CỦA MẤY BÉ
    new()
    {
        Title = "Desert Dunes at Sunset",
        Description = "Rolling sand dunes catch the last golden rays of sunset, creating a mesmerizing pattern of light and shadow across the desert landscape.",
        Url = "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FIMG_2318.jpg&version_id=null",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Desert", "Dunes", "Sunset", "Sand", "Minimalist" },
        Location = "Sahara Desert, Morocco",
        Price = 35000,
        FileName = "desert_dunes_sunset.jpg",
        StoryOfArt = "Golden Waves: The Desert's Shifting Canvas.\n\n1. Environmental Extremes. This photograph was taken in 48°C (118°F) heat, necessitating special thermal protection for both photographer and equipment.\n\n2. Transient Beauty. The specific dune formations shown will no longer exist in their captured form, as desert winds continually reshape the landscape.\n\n3. Color Science. The rich orange hues result from the high iron oxide content in the sand, amplified by the low-angle sunlight at golden hour.\n\n4. Scale Deception. While appearing modest in size, the largest dune in frame stands over 150 meters tall, demonstrating the difficulty of conveying true scale in desert environments.\n\n5. Silent Witness. These dunes have remained largely unchanged in process for over 4,000 years, though their exact shapes shift daily with the wind.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Autumn Forest Path",
        Description = "A winding path cuts through a forest ablaze with autumn colors, creating a tunnel of golden and crimson foliage.",
        Url = "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FIMG_2319.jpg&version_id=null",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "Autumn", "Forest", "Path", "Fall Colors", "Trees" },
        Location = "Vermont, USA",
        Price = 28000,
        FileName = "autumn_forest_path.jpg",
        StoryOfArt = "The Golden Corridor: Autumn's Transient Gallery.\n\n1. Seasonal Peak. This image captures the precise 72-hour window when fall colors reached their maximum vibrancy before beginning to fade and fall.\n\n2. Biological Symphony. The varied colors represent different tree species' unique chemical responses to decreasing daylight - predominantly sugar maples (red), birch (yellow), and oak (russet).\n\n3. Historical Route. The path visible dates to the 18th century and was originally used for transporting maple sap from the forest to local sugar houses.\n\n4. Photographic Technique. To achieve the tunnel effect while maintaining focus throughout, I used focus stacking of seven separate exposures with incrementally different focal points.\n\n5. Climate Concern. Rising regional temperatures have delayed peak foliage season by nearly two weeks compared to historical records from the 1950s.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Coastal Cliff Sunrise",
        Description = "Dramatic sea cliffs catch the first light of day as waves crash against their base, showcasing the raw power of where land meets sea.",
        Url = "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FIMG_2320.jpg&version_id=null",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Coast", "Cliffs", "Ocean", "Sunrise", "Waves" },
        Location = "Moher, Ireland",
        Price = 45000,
        FileName = "coastal_cliff_sunrise.jpg",
        StoryOfArt = "Edge of Worlds: Where Land Confronts Ocean.\n\n1. Geological Testament. These cliffs contain visible sedimentary layers representing over 300 million years of Earth's history, readable like pages in a stone book.\n\n2. Weather Challenge. Capturing this image required enduring near-gale force winds that threatened both equipment stability and photographer safety.\n\n3. Timing Precision. The specific light angle occurs only twice yearly when the sunrise aligns perfectly with the cliff orientation during the spring equinox.\n\n4. Sound Landscape. The thunderous crash of waves against rock created vibrations powerful enough to be felt through the ground at the shooting location 70 meters above sea level.\n\n5. Conservation Significance. This coastline serves as critical nesting habitat for endangered seabird species, with restrictions on human access during breeding seasons.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Mountain Lake Reflection",
        Description = "A pristine alpine lake perfectly mirrors the surrounding mountains and sky, creating a symmetrical landscape of extraordinary beauty.",
        Url = "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FIMG_2321.jpg&version_id=null",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Lake", "Mountains", "Reflection", "Alpine", "Symmetry" },
        Location = "Banff National Park, Canada",
        Price = 38000,
        FileName = "mountain_lake_reflection.jpg",
        StoryOfArt = "Mirror of the Sky: Perfect Alpine Symmetry.\n\n1. Glacial Origin. This lake formed approximately 12,000 years ago during the last ice age when retreating glaciers carved the basin and filled it with pristine meltwater.\n\n2. Rare Conditions. The perfect reflection captured required absolute stillness in both water and air - conditions that occur on fewer than 10 days annually in this high-altitude, typically windy environment.\n\n3. Water Chemistry. The distinctive turquoise color results from 'rock flour' - microscopic glacial sediment suspended in the water that reflects specific light wavelengths.\n\n4. Indigenous Significance. This location holds sacred status for local First Nations peoples, who have traditional stories describing the lake as a portal between worlds.\n\n5. Expedition Challenge. Reaching this remote shooting location required a three-day backcountry hike with all equipment carried by hand, as no vehicle or helicopter access exists.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Urban Night Rain",
        Description = "A busy city intersection glistens with reflections from neon signs and traffic lights, as rain transforms an ordinary urban scene into a colorful abstract canvas.",
        Url = "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FIMG_2329.jpg&version_id=null",
        Orientation = OrientationType.Portrait,
        Tags = new List<string> { "Urban", "Rain", "Night", "City Lights", "Reflection" },
        Location = "Seoul, South Korea",
        Price = 42000,
        FileName = "urban_night_rain.jpg",
        StoryOfArt = "Electric Rain: Urban Symphony After Dark.\n\n1. Cultural Crossroads. This intersection marks the boundary between Seoul's traditional market district and its modern technology hub - a physical manifestation of the country's rapid transformation.\n\n2. Technical Approach. This image employs intentional lens distortion to enhance the surreal quality of the reflections, emphasizing the disorienting experience of urban spaces in adverse weather.\n\n3. Human Element. Though seemingly anonymous, several pedestrians granted permission to be included in the final image, understanding they would become part of an artistic representation of their city.\n\n4. Light Complexity. The scene contains over 200 distinct light sources in various colors and intensities, creating a challenging exposure situation requiring careful dynamic range management.\n\n5. Urban Evolution. This exact view no longer exists as several buildings visible have since been replaced with newer structures - the photograph now serves as historical documentation of a specific moment in the city's development.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
    new()
    {
        Title = "Ancient Olive Grove",
        Description = "Centuries-old olive trees with gnarled trunks stand in formation across a Mediterranean hillside, their silver-green leaves shimmering in the warm light.",
        Url = "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/arwoh-bucket/objects/download?preview=true&prefix=artworks%2FIMG_2327.jpg&version_id=null",
        Orientation = OrientationType.Landscape,
        Tags = new List<string> { "Olives", "Trees", "Mediterranean", "Agriculture", "Ancient" },
        Location = "Puglia, Italy",
        Price = 32000,
        FileName = "ancient_olive_grove.jpg",
        StoryOfArt = "Living Monuments: The Olive Guardians.\n\n1. Botanical Elders. Several trees in this grove have been carbon-dated to be over 1,500 years old, making them living links to the Byzantine period when they were first planted.\n\n2. Agricultural Continuity. The same families have harvested olives from these trees for 27 generations, maintaining traditional farming techniques passed down through centuries.\n\n3. Environmental Adaptation. The twisted forms of the trunks represent the trees' response to prevailing winds and periodic drought - a physical record of environmental conditions spanning millennia.\n\n4. Seasonal Selection. The photograph was taken during the two-week period before harvest when the olives have reached full maturity but remain on the branches.\n\n5. Conservation Challenge. This ancient grove now faces threats from a bacterial disease spreading through the region, potentially endangering trees that have survived countless previous challenges throughout history.",
        PhotographerId = photographers[random.Next(photographers.Count)]
    },
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
                async () => await context.PaymentTransactions.ExecuteDeleteAsync(),
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
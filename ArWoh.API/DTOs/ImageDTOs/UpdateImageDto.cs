using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.ImageDTOs;

public class UpdateImageDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? StoryOfArt { get; set; }
    public OrientationType? Orientation { get; set; }
    public List<string>? Tags { get; set; }
    public string? Location { get; set; }
}
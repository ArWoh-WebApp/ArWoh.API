using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.ImageDTOs;

public class ImageDto
{
    public int Id { get; set; }
    public int? PhotographerId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? StoryOfArt { get; set; }
    public OrientationType? Orientation { get; set; }
    public List<string>? Tags { get; set; }
    public string? Location { get; set; }
    public string? FileName { get; set; }

    public string? Url { get; set; }

    // Thêm thông tin photographer
    public string? PhotographerName { get; set; }
    public string? PhotographerEmail { get; set; }
}
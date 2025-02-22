using System.ComponentModel.DataAnnotations;
using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.ImageDTOs;

public class UploadImageDto
{
    [Required]
    public string Title { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    public decimal Price { get; set; }
    
    public OrientationType? Orientation { get; set; }
    
    public string? Tags { get; set; }
    public string StoryOfArt { get; set; } // Không bắt buộc, có thể null

    [Required]
    public IFormFile File { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace ArWoh.API.DTOs.ImageDTOs;

public class UploadImageDto
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
    public decimal Price { get; set; }

    public string StoryOfArt { get; set; } // Không bắt buộc, có thể null

    [Required]
    public IFormFile File { get; set; }
}

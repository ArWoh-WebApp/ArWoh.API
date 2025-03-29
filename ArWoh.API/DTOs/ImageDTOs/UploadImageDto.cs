using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ArWoh.API.Enums;

namespace ArWoh.API.DTOs.ImageDTOs;

public class UploadImageDto
{
    [Required]
    [DefaultValue("Untitled")] // Default value for Title
    public string Title { get; set; }

    [Required]
    [DefaultValue("No Description")]
    public string Description { get; set; }

    [Required] [DefaultValue(0.0)] public decimal Price { get; set; }

    [DefaultValue(null)] public string? Location { get; set; }

    [DefaultValue(OrientationType.Landscape)]
    public OrientationType? Orientation { get; set; }

    [DefaultValue(null)] public List<string>? Tags { get; set; }
    [DefaultValue("No story provided")] public string StoryOfArt { get; set; } = "No story provided";

    [Required] public IFormFile File { get; set; }
}
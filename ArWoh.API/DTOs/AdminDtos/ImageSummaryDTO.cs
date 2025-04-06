namespace ArWoh.API.DTOs.AdminDtos;

public class ImageSummaryDTO
{
    public int TotalImages { get; set; }
    public Dictionary<string, int> ImageOrientations { get; set; }
}
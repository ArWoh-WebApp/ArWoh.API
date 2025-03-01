using ArWoh.API.Enums;

namespace ArWoh.API.Entities;

public class Image : BaseEntity
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public OrientationType? Orientation { get; set; }
    public List<string>? Tags { get; set; }
    public string? Location { get; set; }
    public string? StoryOfArt { get; set; } // Câu chuyện của ảnh
    public string? FileName { get; set; } // Tên file lưu trên MinIO
    public string? Url { get; set; } // URL lấy từ GetFileUrlAsync()
    public int? PhotographerId { get; set; }

    public User? Photographer { get; set; }
    public ICollection<PaymentTransaction>? Transactions { get; set; }
}
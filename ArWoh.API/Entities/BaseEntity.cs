using System.ComponentModel.DataAnnotations;

namespace ArWoh.API.Entities;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsDeleted { get; set; } = false; // Soft delete flag
    
    public DateTime? DeletedAt { get; set; } // Lưu thời gian xóa
}

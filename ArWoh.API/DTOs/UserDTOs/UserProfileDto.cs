namespace ArWoh.API.DTOs.UserDTOs;

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Bio { get; set; }
    public string ProfilePictureUrl { get; set; }
}
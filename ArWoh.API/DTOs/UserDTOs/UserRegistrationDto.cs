using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ArWoh.API.DTOs.UserDTOs;

public class UserRegistrationDto
{
    [Required]
    [DefaultValue("a@gmail.com")]
    public string Email { get; set; }

    [Required][DefaultValue("1@")] public string Password { get; set; }

    [DefaultValue("NewUser")] public string Username { get; set; } = "NewUser";

    [DefaultValue("No bio provided")] public string Bio { get; set; } = "No bio provided";

    [DefaultValue("https://example.com/default-profile.png")]
    public string ProfilePictureUrl { get; set; } = "https://example.com/default-profile.png";
}
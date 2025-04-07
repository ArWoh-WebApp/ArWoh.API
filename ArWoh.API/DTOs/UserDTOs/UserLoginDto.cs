using System.ComponentModel;

namespace ArWoh.API.DTOs.UserDTOs;

public class UserLoginDto
{
    [DefaultValue("user1@gmail.com")] public string Email { get; set; }
    [DefaultValue("1@")] public string Password { get; set; }
}
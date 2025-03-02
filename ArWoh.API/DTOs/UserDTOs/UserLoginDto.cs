using System.ComponentModel;

namespace ArWoh.API.DTOs.UserDTOs;

public class UserLoginDto
{
    [DefaultValue("a@gmail.com")]
    public string Email { get; set; }
    [DefaultValue("a")]
    public string Password { get; set; }
}
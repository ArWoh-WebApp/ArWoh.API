using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong", status = "API is running!" });
    }
}
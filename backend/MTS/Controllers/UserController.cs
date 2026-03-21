using Microsoft.AspNetCore.Mvc;

namespace MTS.Controllers;

[ApiController]
[Route("/users")]
public class UserController : ControllerBase
{
    [HttpGet("test")]
    public IActionResult Index()
    {
        return Ok(new {});
    }
}
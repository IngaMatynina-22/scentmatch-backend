using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/test")]
[ApiExplorerSettings(IgnoreApi = true)]
public class TestController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public TestController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    private bool IsDevOrTesting =>
        _environment.IsDevelopment() || _environment.IsEnvironment("Testing");

    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult Authenticated()
    {
        if (!IsDevOrTesting)
        {
            return NotFound();
        }

        return Ok(new { message = "You are authenticated." });
    }

    [HttpGet("admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Admin()
    {
        if (!IsDevOrTesting)
        {
            return NotFound();
        }

        return Ok(new { message = "You are an admin." });
    }
}

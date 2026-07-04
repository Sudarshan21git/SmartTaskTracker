using Microsoft.AspNetCore.Mvc;

namespace SmartTaskTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Test API is working!", timestamp = DateTime.UtcNow });
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "OK", message = "Server is running" });
        }
    }
}
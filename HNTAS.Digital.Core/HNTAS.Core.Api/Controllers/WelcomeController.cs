using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WelcomeController : ControllerBase
    {
        /// <summary>
        /// Retrieves a welcome message for the HNTAS Core API.
        /// </summary>
        /// <returns>An HTTP 200 OK with the welcome message string.</returns>
        [HttpGet]   
        [EndpointSummary("Get Welcome Message")] // Updated summary
        [EndpointDescription("This endpoint returns a simple welcome string to confirm the API is running.")] // Updated description
        public IActionResult Get()
        {
            return Ok("Welcome to HNTAS Core API");
        }
    }
}

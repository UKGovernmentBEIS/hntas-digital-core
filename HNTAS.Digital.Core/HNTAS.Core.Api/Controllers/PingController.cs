using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Ping([FromQuery] string conn)
        {
            if (string.IsNullOrWhiteSpace(conn))
                return BadRequest("Missing connection string.");

            try
            {
                var settings = MongoClientSettings.FromUrl(new MongoUrl(conn));
                var client = new MongoClient(settings);
                var database = client.GetDatabase("docdb-HNTAS-dev");
                var collection = database.GetCollection<dynamic>("ping");

                collection.InsertOne(new { message = "Hello DocumentDB", timestamp = DateTime.UtcNow });

                var result = collection.Find(FilterDefinition<dynamic>.Empty).FirstOrDefault();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Connection failed: {ex.Message}");
            }
        }
    }
}

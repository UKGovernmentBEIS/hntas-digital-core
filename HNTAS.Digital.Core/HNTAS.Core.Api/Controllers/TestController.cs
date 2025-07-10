using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public TestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        /// <summary>
        /// Tests the connection to Amazon DocumentDB.
        /// </summary>
        /// <returns>A string indicating the connection status.</returns>
        [HttpGet("test-documentdb-connection")]
        public async Task<IActionResult> TestDocumentDbConnection()
        {
            // Retrieve the connection string from appsettings.json or environment variables.
            // Ensure you have a section like "DocumentDbSettings": { "ConnectionString": "your_documentdb_connection_string" }
            // in your appsettings.json, or an environment variable named DocumentDbSettings__ConnectionString.
            string connectionString = _configuration.GetValue<string>("DocumentDbSettings:ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                return BadRequest("DocumentDB connection string is not configured. Please add 'DocumentDbSettings:ConnectionString' to your appsettings.json or environment variables.");
            }

            // Example connection string format for DocumentDB:
            // "mongodb://<username>:<password>@<cluster-endpoint>:<port>/<database>?replicaSet=rs0&readPreference=secondaryPreferred&retryWrites=false"
            // Remember to replace placeholders with your actual DocumentDB credentials and endpoint.
            // Also, ensure your EC2 instance or local machine has network access to the DocumentDB cluster (security groups).

            try
            {
                // Create a MongoClient instance
                var client = new MongoClient(connectionString);

                // Try to list database names to verify the connection.
                // This operation requires a successful connection.
                await client.ListDatabaseNamesAsync();

                return Ok("Successfully connected to Amazon DocumentDB!");
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error connecting to DocumentDB: {ex.Message}");
                return StatusCode(500, $"Failed to connect to Amazon DocumentDB: {ex.Message}");
            }
        }

    }
}

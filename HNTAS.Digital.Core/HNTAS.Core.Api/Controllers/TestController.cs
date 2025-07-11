using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration; // Ensure this is included
using System; // For Environment.GetEnvironmentVariable

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestController> _logger; // Added for better logging

        // Inject ILogger as well
        public TestController(IConfiguration configuration, ILogger<TestController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Tests the connection to Amazon DocumentDB.
        /// </summary>
        /// <returns>A string indicating the connection status.</returns>
        [HttpGet("test-documentdb-connection")]
        public async Task<IActionResult> TestDocumentDbConnection()
        {
            // Retrieve the connection string from the environment variable.
            // Environment variables are typically case-insensitive on Windows,
            // but case-sensitive on Linux/Unix. It's good practice to match the exact case.
            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");

            _logger.LogInformation("DOCUMENT_DB_CONNECTION_STRING environment : "+ connectionString);

            // Alternatively, ASP.NET Core's configuration system can automatically
            // load environment variables (e.g., if set as "ConnectionStrings__DocumentDb").
            // However, Environment.GetEnvironmentVariable is explicit for a specific variable name.
            // For general config, IConfiguration can still map it if you set up prefixing.
            // Example: string connectionString = _configuration.GetValue<string>("DOCUMENT_DB_CONNECTION_STRING");
            // This relies on the environment variable provider being configured.
            // For a direct read, Environment.GetEnvironmentVariable is simplest.

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("DOCUMENT_DB_CONNECTION_STRING environment variable is not set.");
                return BadRequest("DocumentDB connection string is not configured. Please set the 'DOCUMENT_DB_CONNECTION_STRING' environment variable.");
            }

            // Example connection string format for DocumentDB:
            // "mongodb://<username>:<password>@<cluster-endpoint>:<port>/<database>?replicaSet=rs0&readPreference=secondaryPreferred&retryWrites=false"
            // Remember to replace placeholders with your actual DocumentDB credentials and endpoint.
            // Also, ensure your EC2 instance or local machine has network access to the DocumentDB cluster (security groups).

            try
            {
                var client = new MongoClient(connectionString);

                // Try to list database names to verify the connection.
                await client.ListDatabaseNamesAsync();

                _logger.LogInformation("Successfully connected to Amazon DocumentDB!");
                return Ok("Successfully connected to Amazon DocumentDB!");
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                _logger.LogError(ex, "Error connecting to Amazon DocumentDB using environment variable.");
                return StatusCode(500, $"Failed to connect to Amazon DocumentDB: {ex.Message}");
            }
        }
    }
}
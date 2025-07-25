using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class OrgCounterService : IOrgCounterService
    {
        private readonly IMongoCollection<Counter> _countersCollection;
        private readonly ILogger<OrgCounterService> _logger;

        /// <summary>
        /// Initializes a new instance of the OrgCounterService.
        /// </summary>
        /// <param name="mongoDbSettings">Configuration options for MongoDB settings.</param>
        /// <param name="logger">Logger for logging service operations and errors.</param>
        public OrgCounterService(IOptions<DbSettings> mongoDbSettings, ILogger<OrgCounterService> logger)
        {
            _logger = logger;

            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. " +
                    "Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable");
            }

            if (string.IsNullOrEmpty(mongoDbSettings.Value.DatabaseName))
            {
                _logger.LogCritical("MongoDB DatabaseName is missing in settings. OrgCounterService cannot initialize.");
                throw new InvalidOperationException("MongoDB DatabaseName is not configured. Please check appsettings.json or environment variables.");
            }
            if (string.IsNullOrEmpty(mongoDbSettings.Value.OrgCountersCollectionName))
            {
                _logger.LogCritical("MongoDB OrgCountersCollectionName is missing in settings. OrgCounterService cannot initialize.");
                throw new InvalidOperationException("MongoDB OrgCountersCollectionName is not configured. Please check appsettings.json or environment variables.");
            }


            try
            {
                var mongoClient = new MongoClient(connectionString);
                var mongoDatabase = mongoClient.GetDatabase(mongoDbSettings.Value.DatabaseName);
                _countersCollection = mongoDatabase.GetCollection<Counter>(mongoDbSettings.Value.OrgCountersCollectionName);

                _logger.LogInformation("OrgCounterService initialized successfully. Connected to database '{DatabaseName}', using collection '{CollectionName}'.",
                    mongoDbSettings.Value.DatabaseName, _countersCollection.CollectionNamespace.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to connect to MongoDB for OrgCounterService. Check connection string and MongoDB server status.");
                throw new InvalidOperationException("OrgCounterService failed to connect to MongoDB.", ex);
            }
        }

        /// <summary>
        /// Atomically increments a sequence counter and returns its new value.
        /// This method is crucial for generating unique, sequential IDs in MongoDB.
        /// It uses findOneAndUpdate with $inc and upsert:true for atomicity and initialization.
        /// </summary>
        /// <param name="sequenceName">The unique name of the sequence (e.g., "userId_sequence", "orgId_sequence").</param>
        /// <returns>The incremented sequence value.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the counter cannot be retrieved or incremented.</exception>
        public async Task<long> GetNextSequenceValue(string sequenceName)
        {
            _logger.LogDebug("Attempting to get next sequence value for counter: '{SequenceName}'.", sequenceName);

            // Filter to find the specific counter document by its _id
            var filter = Builders<Counter>.Filter.Eq(c => c.Id, sequenceName);

            // Update operation: increment the 'sequence_value' field by 1
            var update = Builders<Counter>.Update.Inc(c => c.SequenceValue, 1);

            // Options for the findOneAndUpdate operation
            var options = new FindOneAndUpdateOptions<Counter, Counter>
            {
                ReturnDocument = ReturnDocument.After, // Return the document *after* the update has been applied
                IsUpsert = true // If a document matching the filter doesn't exist, create it.
                                // For a new counter, SequenceValue will effectively start at 0, then become 1.
            };

            try
            {
                // Execute the atomic findOneAndUpdate operation
                var counter = await _countersCollection.FindOneAndUpdateAsync(filter, update, options);

                // Defensive check: counter should never be null if IsUpsert is true and operation is successful
                if (counter == null)
                {
                    _logger.LogError("Failed to retrieve or create counter document for sequence '{SequenceName}' despite upsert option. This is unexpected.", sequenceName);
                    throw new InvalidOperationException($"Failed to get or create counter for sequence '{sequenceName}'. The database operation did not return a document.");
                }

                _logger.LogDebug("Next sequence value for '{SequenceName}' is {SequenceValue}.", sequenceName, counter.SequenceValue);
                return counter.SequenceValue;
            }
            catch (MongoException ex) // Catch specific MongoDB driver exceptions
            {
                _logger.LogError(ex, "MongoDB error occurred while getting next sequence value for '{SequenceName}'.", sequenceName);
                throw new InvalidOperationException($"Database error generating sequence '{sequenceName}'. See inner exception for details.", ex);
            }
            catch (Exception ex) // Catch any other unexpected exceptions
            {
                _logger.LogError(ex, "An unexpected error occurred while getting next sequence value for '{SequenceName}'.", sequenceName);
                throw new InvalidOperationException($"Failed to generate sequence '{sequenceName}' due to an unexpected error. See inner exception for details.", ex);
            }
        }
    }
}


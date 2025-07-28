using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class HeatNetworkService : IHeatNetworkService
    {
        private readonly IMongoCollection<HeatNetwork> _hnCollection;
        private readonly ILogger<HeatNetworkService> _logger;

        public HeatNetworkService(IOptions<AWSDocDbSettings> dbSettings, ILogger<HeatNetworkService> logger)
        {
            _logger = logger;
            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");

            _logger.LogInformation("Initializing UserService with connection string : " + connectionString);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. " +
                    "Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable");
            }
            var mongoClient = new MongoClient();
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
            _hnCollection = mongoDatabase.GetCollection<HeatNetwork>(dbSettings.Value.HeatNetworksCollectionName);
           
        }

        public async Task CreateAsync(HeatNetwork newHeatNetwork) =>
            await _hnCollection.InsertOneAsync(newHeatNetwork);

        public async Task<List<HeatNetwork>> GetAsync()
        {
           return await _hnCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<HeatNetwork>> GetByHnIdsAsync(List<string> hnIds)
        {
            var filter = Builders<HeatNetwork>.Filter.In(hn => hn.hn_id, hnIds);
            return await _hnCollection.Find(filter).ToListAsync();
        }
    }
}

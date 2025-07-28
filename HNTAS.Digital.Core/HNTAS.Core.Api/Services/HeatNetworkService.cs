using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class HeatNetworkService : IHeatNetworkService
    {
        private readonly IMongoCollection<HeatNetwork> _hnCollection;
        

        public HeatNetworkService(IOptions<DbSettings> dbSettings)
        {
            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
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


    }
}

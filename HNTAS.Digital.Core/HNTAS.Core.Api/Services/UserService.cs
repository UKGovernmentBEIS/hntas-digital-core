using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly ILogger<UserService> _logger;

        public UserService(IOptions<AWSDocDbSettings> dbSettings, ILogger<UserService> logger)
        {

            _logger = logger;

            string? connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");

            _logger.LogDebug("Initializing UserService with connection string: {ConnectionString}", connectionString);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("MongoDB connection string is not configured. " +
                    "Set 'DOCUMENT_DB_CONNECTION_STRING' environment variable");
            }
            var mongoClient = new MongoClient();
            var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
            _usersCollection = mongoDatabase.GetCollection<User>(dbSettings.Value.UsersCollectionName);
           
        }

        // Get all users
        public async Task<List<User>> GetAsync() =>
            await _usersCollection.Find(_ => true).ToListAsync();

        // Get user by ID (MongoDB's internal ObjectId)
        public async Task<User> GetByIdAsync(string id) =>
            await _usersCollection.Find(user => user.Id == id).FirstOrDefaultAsync();

        // Get user by custom user_id
        public async Task<User> GetByUserOneLoginIdAsync(string oneLoginId) =>
            await _usersCollection.Find(user => user.OneLoginId == oneLoginId).FirstOrDefaultAsync();

        // Add a new user
        public async Task CreateAsync(User newUser) =>
            await _usersCollection.InsertOneAsync(newUser);

        // Update an existing user (by MongoDB's internal ObjectId)
        public async Task UpdateAsync(string id, User updatedUser) =>
            await _usersCollection.ReplaceOneAsync(user => user.Id == id, updatedUser);

        // Delete a user (by MongoDB's internal ObjectId)
        public async Task RemoveAsync(string id) =>
            await _usersCollection.DeleteOneAsync(user => user.Id == id);
    }
}

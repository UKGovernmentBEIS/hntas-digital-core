using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Data.Models.Arms.Submission;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Arms.PowerBi;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{

    public class ArmsPowerBiService : IArmsPowerBiService
    {
        private readonly IMongoCollection<KpiSubmission> _kpiCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly ILogger<ArmsPowerBiService> _logger;
        private readonly AWSDocDbSettings _awsdocDbSettings;

        public ArmsPowerBiService(ILogger<ArmsPowerBiService> logger, IMongoDatabase mongoDatabase, IOptions<AWSDocDbSettings> dbSettings)
        {
            _awsdocDbSettings = dbSettings.Value;
            _kpiCollection = mongoDatabase.GetCollection<KpiSubmission>(_awsdocDbSettings.KPI_DataCollectionName);
            _usersCollection = mongoDatabase.GetCollection<User>(dbSettings.Value.UsersCollectionName);
            _logger = logger;
        }

        public async Task<List<ArmsPowerBiReportResult>> GetPowerBiDataAsync()
        {
            try
            {
                var pipeline = _kpiCollection.Aggregate()
                    // 1. Join KpiSubmission to HeatNetwork using the configured collection name
                    .AppendStage<BsonDocument>(new BsonDocument("$lookup", new BsonDocument
                    {
                    { "from", _awsdocDbSettings.HeatNetworksCollectionName },
                    { "localField", "metaData.networkId" },
                    { "foreignField", "hnId" },
                    { "as", "networkData" }
                    }))
                    .Unwind("networkData")

                    // 2. Join HeatNetwork to Organisation using the configured collection name
                    .AppendStage<BsonDocument>(new BsonDocument("$lookup", new BsonDocument
                    {
                    { "from", _awsdocDbSettings.OrganisationsCollectionName },
                    { "localField", "networkData.orgId" },
                    { "foreignField", "orgId" },
                    { "as", "orgData" }
                    }))
                    .Unwind("orgData")

                    // 3. Project clean fields to avoid class deserialization issues
                    .Project<ArmsPowerBiReportResult>(new BsonDocument
                    {
                    { "_id", 0 },
                    { "OrgId", "$orgData.orgId" },
                    // Explicitly pull out only the clean KPI fields from the modified root
                    { "KpiSubmission", new BsonDocument
                        {
                            { "_id", new BsonDocument("$toString", "$_id") },
                            { "metaData", "$metaData" },
                            { "elements", "$elements" },
                            { "consumerConnectionAggregatedKpis", "$consumerConnectionAggregatedKpis" }
                        }
                    }
                    });

                return await pipeline.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Power BI aggregation pipeline.");
                throw;
            }
        }


        public async Task<List<ArmsPowerBiUserReportResult>> GetPowerBiUserDataAsync()
        {
            try
            {
                var pipeline = _usersCollection.Aggregate()
                    // 1. Filter for Active users only (matches the string stored in DB)
                    .Match(new BsonDocument("status", UserStatus.Active.ToString()))
                    .Match(new BsonDocument("roles", UserRole.ResponsibleParty.ToString()))

                    // 2. Deconstruct the hnRoleMappings array to handle users with multiple heat networks
                    .Unwind("hnRoleMappings", new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = false })

                    // 3. Join with the Heat Networks collection
                    .AppendStage<BsonDocument>(new BsonDocument("$lookup", new BsonDocument
                    {
                        { "from", _awsdocDbSettings.HeatNetworksCollectionName },
                        { "localField", "hnRoleMappings.hnId" },
                        { "foreignField", "hnId" },
                        { "as", "networkData" }
                    }))
                    .Unwind("networkData")

                    // 4. Join with the Organisations collection using the organization ID linked to the network
                    .AppendStage<BsonDocument>(new BsonDocument("$lookup", new BsonDocument
                    {
                        { "from", _awsdocDbSettings.OrganisationsCollectionName },
                        { "localField", "networkData.orgId" },
                        { "foreignField", "orgId" },
                        { "as", "orgData" }
                    }))
                    .Unwind("orgData")

                    // 5. Project clean, flat fields into our result class
                    .Project<ArmsPowerBiUserReportResult>(new BsonDocument
                    {
                        { "_id", 0 },
                        { "UserId", new BsonDocument("$toString", "$_id") },
                        { "HnId", "$networkData.hnId" },
                        { "OrgId", "$orgData.orgId" }
                    });

                return await pipeline.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Power BI User aggregation pipeline.");
                throw;
            }
        }
    }
}

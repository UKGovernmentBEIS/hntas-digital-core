using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Services
{
    public class AuditService : IAuditService
    {
        private readonly ILogger<AuditService> _logger;
        private readonly IMongoDatabase _mongoDatabase;

        private const int DefaultPageNumber = 1;
        private const int DefaultPageSize = 6;
        private const string DefaultSortBy = "timestamp";

        public AuditService(ILogger<AuditService> logger, IMongoDatabase mongoDatabase)
        {
            _logger = logger;
            _mongoDatabase = mongoDatabase;
            _logger.LogInformation("AuditService (Per-Collection Pattern) initialized.");
        }

        public async Task SaveAuditAsync<T>(
            string entryType,
            string actorId,
            string entityId,
            T? oldState,
            T? newState,            
            string elementName,
            string phase,
            string stage,
            string? changeNote = null
            )
        {
            try
            {
                // Resolve collection name: e.g., "Audit_HeatNetworks" or "Audit_Assessors"
                var collectionName = $"Audit_{typeof(T).Name}s";
                var collection = _mongoDatabase.GetCollection<AuditEntry<T>>(collectionName);

                var entry = new AuditEntry<T>
                {
                    EntryType = entryType,
                    EntityId = entityId,
                    UserId = actorId,
                    Before = oldState,
                    After = newState,
                    ChangeNote = changeNote,
                    Timestamp = DateTime.UtcNow,                    
                    ElementName = elementName,
                    Phase = phase,
                    Stage = stage
                };

                await collection.InsertOneAsync(entry);
                _logger.LogInformation("Audit event {EntryType} recorded in {Collection}", StringFormatter.Sanitize(entryType), StringFormatter.Sanitize(collectionName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record audit event {EntryType} for entity {EntityId}", StringFormatter.Sanitize(entryType), StringFormatter.Sanitize(entityId));
                // In a POC, we usually don't want an audit failure to crash the main business flow,
                // but you may choose to re-throw based on compliance needs.
            }
        }

        //public async Task<List<AuditEntry<T>>> GetHistoryAsync<T>(string entityId)
        //{
        //    var collectionName = $"Audit_{typeof(T).Name}s";
        //    var collection = _mongoDatabase.GetCollection<AuditEntry<T>>(collectionName);

        //    // Return history sorted by newest first
        //    return await collection
        //        .Find(x => x.EntityId == entityId)
        //        .SortByDescending(x => x.Timestamp)
        //        .ToListAsync();
        //}


        public async Task<AuditLogResponse> GetAuditHistoryAsync<T>(AuditLogRequest auditLogRequest)
        {
            // Validate pagination parameters
            if (auditLogRequest.Page < 1) auditLogRequest.Page = DefaultPageNumber;
            if (auditLogRequest.PageSize < 1) auditLogRequest.PageSize = DefaultPageSize;

            // 1. Determine collection names
            var collectionName = $"Audit_{typeof(T).Name}s";
            var auditCollection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);

            // London Time Zone for UK compliance
            var londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            // 2. Get total count for pagination metadata
            var totalCount = await auditCollection.CountDocumentsAsync(new BsonDocument("entityId", auditLogRequest.HnId));

            // 3. Build Aggregation Pipeline with pagination
            var sortBy = ColumnNameForSorting(auditLogRequest.SortBy!) ?? DefaultSortBy;
            var sortDirection = auditLogRequest.SortDirection?.ToLowerInvariant() ?? "desc";
            var sortDefinition = sortDirection == "asc"
                ? Builders<BsonDocument>.Sort.Ascending(sortBy)
                : Builders<BsonDocument>.Sort.Descending(sortBy);

            var pipeline = auditCollection.Aggregate()
                .Match(new BsonDocument("entityId", auditLogRequest.HnId))
                // Join with Users collection
                .Lookup("Users", "userId", "_id", "joinedUser")
                // Flatten the joinedUser array (left outer join)
                .Unwind("joinedUser", new AggregateUnwindOptions<BsonDocument> { PreserveNullAndEmptyArrays = true })
                .Sort(sortDefinition)
                .Skip((auditLogRequest.Page - 1) * auditLogRequest.PageSize)
                .Limit(auditLogRequest.PageSize);

            var results = await pipeline.ToListAsync();

            // 4. Map to DTO
            var auditLogs = results.Select(doc =>
            {
                var userDoc = doc.Contains("joinedUser") && !doc["joinedUser"].IsBsonNull
                              ? doc["joinedUser"].AsBsonDocument
                              : null;

                string roleDescription = "N/A";

                if (userDoc != null)
                {
                    // PRIORITY 1: Check specific Heat Network role mappings (per GetHeatNetworksForUser logic)
                    bool foundSpecificMapping = false;
                    if (userDoc.Contains("hnRoleMappings") && userDoc["hnRoleMappings"].IsBsonArray)
                    {
                        var mapping = userDoc["hnRoleMappings"].AsBsonArray
                            .Select(m => m.AsBsonDocument)
                            .FirstOrDefault(m => m.Contains("hnId") && m["hnId"].AsString == auditLogRequest.HnId);

                        if (mapping != null && Enum.TryParse(mapping["role"].AsString, out ContributorRole hnRole))
                        {
                            roleDescription = hnRole.GetDescription();
                            foundSpecificMapping = true;
                        }
                    }

                    // PRIORITY 2: If no specific mapping, check for "Full Access" Org roles (RP or NetworkManager)
                    if (!foundSpecificMapping && userDoc.Contains("roles") && userDoc["roles"].IsBsonArray)
                    {
                        var globalRoles = userDoc["roles"].AsBsonArray.Select(r => r.AsString).ToList();

                        // Check for Responsible Party first, then NetworkManager
                        if (globalRoles.Contains(UserRole.ResponsibleParty.ToString()))
                        {
                            roleDescription = UserRole.ResponsibleParty.GetDescription();
                        }
                        else if (globalRoles.Contains(UserRole.NetworkManager.ToString()))
                        {
                            roleDescription = UserRole.NetworkManager.GetDescription();
                        }
                    }
                }

                return new AuditLog
                {
                    EntryType = doc.Contains("entryType") ? doc["entryType"].AsString : "Unknown",
                    UserName = userDoc != null
                        ? $"{userDoc.GetValue("firstName", string.Empty)} {userDoc.GetValue("lastName", string.Empty)}".Trim()
                        : "System Process",
                    Role = roleDescription,
                    Timestamp = TimeZoneInfo.ConvertTimeFromUtc(doc["timestamp"].ToUniversalTime(), londonTimeZone)
                                            .ToString("dd MMM yyyy HH:mm:ss"),
                    ElementName = doc.Contains("elementName") ? doc["elementName"].AsString : "Unknown",
                    Phase = doc.Contains("phase") ? doc["phase"].AsString : "Unknown",
                    Stage = doc.Contains("stage") ? doc["stage"].AsString : "Unknown"
                };
            }).ToList();

            // 5. Return paginated response
            return new AuditLogResponse
            {
                HnId = auditLogRequest.HnId,
                Items = auditLogs,
                PageNumber = auditLogRequest.Page,
                PageSize = auditLogRequest.PageSize,
                TotalCount = (int)totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)auditLogRequest.PageSize)
            };
        }

        private string ColumnNameForSorting(string sortBy)
        {
            switch (sortBy?.ToLowerInvariant())
            {
                case "timestamp":
                    return "timestamp";
                case "phase":
                    return "phase"; 
                case "entrytype":
                    return "entryType";
                case "element":
                    return "elementName";
                default:
                    return "timestamp";
                }
            }
        
    }
    
}

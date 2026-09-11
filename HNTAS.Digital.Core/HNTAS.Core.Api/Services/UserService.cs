using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Data;
using System.Formats.Asn1;

namespace HNTAS.Core.Api.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Organisation> _OrgCollection;
        private readonly IMongoCollection<Invitation> _invitationsCollection;

        public UserService(
        IMongoDatabase mongoDatabase,
        IOptions<AWSDocDbSettings> dbSettings,
        ILogger<UserService> logger)
        {
            _logger = logger;
            _usersCollection = mongoDatabase.GetCollection<User>(dbSettings.Value.UsersCollectionName);
            _OrgCollection = mongoDatabase.GetCollection<Organisation>(dbSettings.Value.OrganisationsCollectionName);
            _invitationsCollection = mongoDatabase.GetCollection<Invitation>(dbSettings.Value.InvitationsCollectionName);
            _logger.LogInformation("UserService initialized via Dependency Injection.");
        }

        public async Task<List<User>> GetAsync() =>
            await _usersCollection.Find(FilterDefinition<User>.Empty).ToListAsync();

        public async Task<User?> GetByIdAsync(string id) =>
            await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task<User?> GetByEmailAsync(string emailId) =>
          await _usersCollection.Find(u => u.EmailId == emailId).FirstOrDefaultAsync();

        public async Task<User?> GetByUserOneLoginIdAsync(string oneLoginId) =>
            await _usersCollection.Find(u => u.OneLoginId == oneLoginId).FirstOrDefaultAsync();

        public async Task CreateAsync(User newUser) =>
            await _usersCollection.InsertOneAsync(newUser);

        public async Task UpdateAsync(string id, User updatedUser) =>
            await _usersCollection.ReplaceOneAsync(u => u.Id == id, updatedUser);

        public async Task RemoveAsync(string id) =>
            await _usersCollection.DeleteOneAsync(u => u.Id == id);

        public async Task<List<User>> GetRegisteredUsers(List<string> invitedEmails) =>
             await _usersCollection.Find(u => invitedEmails.Contains(u.EmailId)).ToListAsync();

        public async Task<List<User>> GetAssessorsByHnIdAsync(string hnId)
        {
            var filter = Builders<User>.Filter.ElemMatch(
                u => u.HnRoleMappings,
                mapping => mapping.HnId == hnId && mapping.Role == ContributorRole.Assessor
            );

            return await _usersCollection.Find(filter).ToListAsync();
        }

        public async Task<List<User>> GetUsersAssociatedByHnIdAsync(string hnId)
        {
            var filter = Builders<User>.Filter.ElemMatch(
                u => u.HnRoleMappings,
                mapping => mapping.HnId == hnId
            );

            return await _usersCollection.Find(filter).ToListAsync();
        }

        public async Task<User?> GetResponsiblePartyByHnIdAsync(string hnId)
        {

            // Filter to find the Organisation whose HnIds array contains the target hnId
            var orgFilter = Builders<Organisation>.Filter.AnyEq(o => o.HnIds, hnId);

            // Select only the RpUserId field to keep the query light
            var projection = Builders<Organisation>.Projection.Include(o => o.RpUserId);

            var organisation = await _OrgCollection
                                       .Find(orgFilter)
                                       .Project<Organisation>(projection)
                                       .FirstOrDefaultAsync();

            // Check if an organisation was found or if it has an RP assigned
            if (organisation == null || string.IsNullOrEmpty(organisation.RpUserId))
            {
                _logger.LogWarning("No organisation found for HN ID {HnId} or RpUserId is missing.", hnId);
                return null;
            }


            // Filter by the RpUserId from the organisation
            var userFilter = Builders<User>.Filter.Eq(u => u.Id, organisation.RpUserId);

            // *Optional secondary check*: Ensure the user also has the ResponsibleParty role
            var roleCheck = Builders<User>.Filter.AnyEq(u => u.Roles, UserRole.ResponsibleParty);

            var finalFilter = Builders<User>.Filter.And(userFilter, roleCheck);

            return await _usersCollection.Find(finalFilter).FirstOrDefaultAsync();
        }


        public async Task<List<User>> GetContributorsByHnIdAsync(string hnId)
        {
            var filter = Builders<User>.Filter.ElemMatch(
                u => u.HnRoleMappings,
                mapping => mapping.HnId == hnId
            );

            return await _usersCollection.Find(filter).ToListAsync();
        }

        public async Task<List<UserRoleDetailResponse>> GetHeatNetworkUsersWithRolesAsync(string hnId)
        {
            var pipeline = new[]
            {
                // 1. Unwind the HnRoleMappings array
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$hnRoleMappings" },
                    { "preserveNullAndEmptyArrays", false }
                }),

                // 2. Match only mappings with the specified Heat Network ID
                new BsonDocument("$match", new BsonDocument("hnRoleMappings.hnId", hnId)),

                // 3. Project only the necessary fields
                new BsonDocument("$project", new BsonDocument
                {
                    { "userId", "$_id" },
                    { "firstName", 1 },
                    { "lastName", 1 },
                    { "emailId", 1 },
                    { "role", "$hnRoleMappings.role" }
                })
            };

            var results = await _usersCollection
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync();

            return results.Select(doc => new UserRoleDetailResponse
            {
                FullName = $"{StringFormatter.ToTitleCaseSingleWord(doc["firstName"].ToString())} {StringFormatter.ToTitleCaseSingleWord(doc["lastName"].ToString())}",
                UserId = doc["userId"].ToString(),
                EmailId = doc["emailId"].ToString(),
                RoleDescription = Enum.TryParse<ContributorRole>(doc["role"].ToString(), out var role) ? role.GetDescription() : "Unknown Role"
            }).ToList();
        }

        public async Task<UpdateResult> UpdateOrgIdAsync(string userId, string orgId)
                        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);

            var update = Builders<User>.Update.Set(u => u.OrgId, orgId);

            return await _usersCollection.UpdateOneAsync(filter, update);
        }


        public async Task<List<User>> GetUsersByOrgIdAsync(string organisationId)
        {
            var filter = Builders<User>.Filter.Eq(u => u.OrgId, organisationId);

            return await _usersCollection
                .Find(filter)
                .ToListAsync();
        }

        public async Task<List<User>> GetActiveNetworkManagersByRpUserIdAsync(string rpUserId)
        {
            // find invitations where inviterUserId: ObjectId(rpUserId), invitedRoles: [ 'NetworkManager' ] and status: 'Accepted' , in that order
            ;            
            var networkManagerInvitations = await _invitationsCollection.Find(u =>
                u.InviterUserId == rpUserId &&
                u.InvitedRoles.Contains(ContributorRole.NetworkManager) &&
                u.Status == InvitationStatus.Accepted
            ).ToListAsync();
            var networkManagers = new List<User>();
            foreach(var invitation in networkManagerInvitations)
            {
                var user = await _usersCollection.Find(u => u.EmailId == invitation.InvitedEmail).FirstOrDefaultAsync();
                if (user != null && user.Status == UserStatus.Active)
                {
                    networkManagers.Add(user);
                }
            }
            return networkManagers;
        }

        public async Task<UserDetailsResult> GetUserWithDetailsAsync(string userId)
        {
            var userObjectId = ObjectId.Parse(userId);

            // 1. Define the specific $match stage for a single user ID
            var matchStage = new BsonDocument("$match", new BsonDocument("_id", userObjectId));

            // 2. Execute the common pipeline
            using (var cursor = GetUsersDetailsPipeline(matchStage))
            {
                return await cursor.FirstOrDefaultAsync();
            }

        }


        public async Task<List<UserDetailsResult>> GetUsersByInvitedEmailsWithDetailsAsync(List<string> invitedEmails)
        {
            // 1. Define the specific $match stage for multiple email IDs
            var matchStage = new BsonDocument("$match",
                new BsonDocument("emailId", new BsonDocument("$in", new BsonArray(invitedEmails))));

            using (var cursor = GetUsersDetailsPipeline(matchStage))
            {
                return await cursor.ToListAsync();
            }
        }

        public async Task UpdateUserNetwork(string userId, string hnId, ContributorRole role = ContributorRole.ResponsibleParty)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            // update hnRoleMappings array by adding a new mapping with the provided hnId and a default role (e.g., RP)
            var update = Builders<User>.Update.AddToSet(u => u.HnRoleMappings, new HnRoleMapping
            {
                HnId = hnId,
                Role = role
            });
            var result = await _usersCollection.UpdateOneAsync(filter, update);

            _logger.LogInformation(
                "Matched={MatchedCount}, Modified={ModifiedCount}",
                result.MatchedCount,
                result.ModifiedCount);
        }

        // Check if the users have active status against Users collection, if not then update the status to Invited in the response object
        public async Task<List<ManagedUserResponse>> GetActiveUsers(List<ManagedUserResponse> users)
        {
            if (users == null || users.Count == 0)
            {
                return new List<ManagedUserResponse>();
            }

            var emails = users
                .Where(u => !string.IsNullOrWhiteSpace(u.EmailId))
                .Select(u => u.EmailId)
                .Distinct()
                .ToList();

            if (emails.Count == 0)
            {
                return users;
            }

            var activeEmails = await _usersCollection
                .Find(u => emails.Contains(u.EmailId) && u.Status == UserStatus.Active)
                .Project(u => u.EmailId)
                .ToListAsync();

            var activeEmailSet = new HashSet<string>(activeEmails, StringComparer.OrdinalIgnoreCase);

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.EmailId) || !activeEmailSet.Contains(user.EmailId))
                {
                    user.Status = InvitationStatus.Invited.ToString();
                }
                else
                {
                    user.Status = UserStatus.Active.ToString();
                }
            }

            return users;
        }

        // --- Private Helper Method for Reusable Pipeline ---

        /// <summary>
        /// Executes the common MongoDB aggregation pipeline to fetch user details.
        /// </summary>
        private IAsyncCursor<UserDetailsResult> GetUsersDetailsPipeline(BsonDocument matchStage)
        {
            var pipeline = new List<BsonDocument>
            {
                // The dynamic filter stage
                matchStage,

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Organisations" },
                    { "localField", "orgId" },
                    { "foreignField", "orgId" },
                    { "as", "organisationDetails" }
                }),

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$organisationDetails" },
                    { "preserveNullAndEmptyArrays", true }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "HeatNetworks" },
                    { "localField", "organisationDetails.hnIds" },
                    { "foreignField", "hnId" },
                    { "as", "organisationHeatNetworkDetails" }
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "HeatNetworks" },
                    { "localField", "hnRoleMappings.hnId" },
                    { "foreignField", "hnId" },
                    { "as", "mappedHeatNetworks" }
                }),

                // Final projection into DTO shape (unchanged)
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", new BsonDocument("$toString", "$_id") },
                    { "oneloginId", 1 },
                    { "firstName", 1 },
                    { "lastName", 1 },
                    { "emailId", 1 },
                    { "jobTitle", 1 },
                    { "preferredContactType", 1 },
                    { "landlineNumber", 1 },
                    { "contactNumberExtension", 1 },
                    { "mobileNumber", new BsonDocument("$ifNull", new BsonArray { "$mobileNumber", BsonNull.Value }) },
                    { "roles", 1 },
                    { "status", 1 },

                    // Organisation projection
                    { "organisation", new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$or", new BsonArray {
                            new BsonDocument("$eq", new BsonArray { "$organisationDetails", BsonNull.Value }),
                            new BsonDocument("$not", "$organisationDetails")
                        }) },
                        { "then", BsonNull.Value },
                        { "else", new BsonDocument
                            {
                                { "orgId", "$organisationDetails.orgId" },
                                { "name", "$organisationDetails.name" },
                                { "companiesHouseNumber", "$organisationDetails.companiesHouseNumber" },
                                { "type", "$organisationDetails.type" },
                                { "registeredAddress", "$organisationDetails.registeredAddress" },

                                // Heat networks nested inside organisation
                                { "heatNetworks", new BsonDocument("$map", new BsonDocument
                                    {
                                        { "input", "$organisationHeatNetworkDetails" },
                                        { "as", "hn" },
                                        { "in", new BsonDocument
                                            {
                                                { "hnId", "$$hn.hnId" },
                                                { "name", "$$hn.name" },
                                                { "location", "$$hn.location" }
                                            }
                                        }
                                    })
                                }
                            }
                        }
                    }) },

                    // HnRoleMappings projection
                    { "hnRoleMappings", new BsonDocument("$map", new BsonDocument
                        {
                            { "input", "$hnRoleMappings" },
                            { "as", "mapping" },
                            { "in", new BsonDocument
                                {
                                    { "role", "$$mapping.role" },
                                    { "heatNetwork", new BsonDocument("$let", new BsonDocument
                                        {
                                            { "vars", new BsonDocument("matchedHn", new BsonDocument("$arrayElemAt", new BsonArray
                                                {
                                                    new BsonDocument("$filter", new BsonDocument
                                                        {
                                                            { "input", "$mappedHeatNetworks" },
                                                            { "as", "details" },
                                                            { "cond", new BsonDocument("$eq", new BsonArray { "$$details.hnId", "$$mapping.hnId" }) }
                                                        }),
                                                    0
                                                }))
                                            },
                                            { "in", new BsonDocument("$cond", new BsonArray
                                                {
                                                    new BsonDocument("$not", new BsonArray { "$$matchedHn" }),
                                                    BsonNull.Value,
                                                    new BsonDocument
                                                        {
                                                            { "hnId", "$$matchedHn.hnId" },
                                                            { "name", "$$matchedHn.name" },
                                                            { "location", "$$matchedHn.location" }
                                                        }
                                                })
                                            }
                                        })
                                    }
                                }
                            }
                        })
                    }
                })
            };

            return _usersCollection.Aggregate<UserDetailsResult>(pipeline);
        }        
    }
}

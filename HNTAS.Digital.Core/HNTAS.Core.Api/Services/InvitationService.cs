using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Data;

namespace HNTAS.Core.Api.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IMongoCollection<Invitation> _invitationsCollection;
        private readonly ILogger<InvitationService> _logger;
        private readonly IUserService _userService;
        private readonly IHeatNetworkService _heatNetworkService;
        private readonly IAuditService _auditService;
        private readonly INotificationHistoryService _notificationHistoryService;

        public InvitationService(
        IMongoDatabase mongoDatabase,
        IOptions<AWSDocDbSettings> dbSettings,
        ILogger<InvitationService> logger,
        IUserService userService,
        IHeatNetworkService heatNetworkService,
        IAuditService auditService,
        INotificationHistoryService notificationHistoryService)
        {
            _logger = logger;
            _invitationsCollection = mongoDatabase.GetCollection<Invitation>(dbSettings.Value.InvitationsCollectionName);
            _logger.LogInformation("UserService initialized via Dependency Injection.");
            _userService = userService;
            _heatNetworkService = heatNetworkService;
            _auditService = auditService;
            _notificationHistoryService = notificationHistoryService;
        }

        // Get all invitations
        public async Task<List<Invitation>> GetAsync() =>
            await _invitationsCollection.Find(_ => true).ToListAsync();

        // Get invitation by ID
        public async Task<Invitation> GetByIdAsync(string id) =>
            await _invitationsCollection.Find(invitation => invitation.Id == id).FirstOrDefaultAsync();

        // Get invitations by InviterUserId
        public async Task<List<Invitation>> GetByInviterUserIdAsync(string inviterUserId) =>
            await _invitationsCollection.Find(invitation => invitation.InviterUserId == inviterUserId).ToListAsync();

        // Create a new invitation
        public async Task CreateAsync(Invitation newInvitation) =>
            await _invitationsCollection.InsertOneAsync(newInvitation);

        // Update an existing invitation
        public async Task UpdateAsync(string id, Invitation updatedInvitation) =>
            await _invitationsCollection.ReplaceOneAsync(invitation => invitation.Id == id, updatedInvitation);

        // Remove an invitation by ID
        //public async Task RemoveAsync(string id) =>
        //    await _invitationsCollection.DeleteOneAsync(invitation => invitation.Id == id);

        public async Task<Invitation> GetByEmailAsync(string invitedEmail, string hnId) =>
          await _invitationsCollection
              .Find(invitation =>
                  invitation.InvitedEmail == invitedEmail &&
                  invitation.InvitedHnId == hnId &&
                  invitation.Status == Enums.InvitationStatus.Invited)
              .SortByDescending(invitation => invitation.InvitedAt)
              .FirstOrDefaultAsync();

        public async Task<List<Invitation>> GetByEmailsAndHnIdAsync(List<string> invitedEmails, string hnId) =>
             await _invitationsCollection
                .Find(invitation =>
                    invitedEmails.Contains(invitation.InvitedEmail) &&
                    invitation.InvitedHnId == hnId &&
                    invitation.Status == Enums.InvitationStatus.Accepted)
                .ToListAsync();

        public async Task<List<ManagedUserResponse>> GetInvitedUsersAsRegisteredAsync(string inviterUserId)
        {
            var inviterObjectId = ObjectId.Parse(inviterUserId);

            var pipeline = new[]
            {
                // Match invitations sent by the specified user
                new BsonDocument("$match", new BsonDocument("inviterUserId", inviterObjectId)),

                // Lookup heat network name using invitedHnId
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "HeatNetworks" },
                    { "localField", "invitedHnId" },
                    { "foreignField", "hnId" },
                    { "as", "heatNetworkDetails" }
                }),

                // Sort by invitedAt descending
                new BsonDocument("$sort", new BsonDocument("invitedAt", -1)),

                // Project into RegisteredUserResponse shape
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", new BsonDocument("$toString", "$_id") },
                    { "name", new BsonDocument("$concat", new BsonArray { "$firstName", " ", "$lastName" }) },
                    { "emailId", "$invitedEmail" },
                    { "invitedAt", "$invitedAt" },
                    { "status", new BsonDocument("$toString", "$status") },
                    { "roles", new BsonDocument("$map", new BsonDocument
                        {
                            { "input", "$invitedRoles" },
                            { "as", "role" },
                            { "in", new BsonDocument("$toString", "$$role") }
                        })
                    },
                    { "heatNetworks", new BsonDocument("$map", new BsonDocument
                        {
                            { "input", "$heatNetworkDetails" },
                            { "as", "hn" },
                            { "in", new BsonDocument
                                {
                                    { "hnId", "$$hn.hnId" },
                                    { "name", "$$hn.name" }
                                }
                            }
                        })
                    }
                })
            };

            return await _invitationsCollection
                .Aggregate<ManagedUserResponse>(pipeline)
                .ToListAsync();
        }

        // Get invitation by invitedEmailId, invitedHnId, invitedRole
        public async Task<Invitation> GetByInvitedDetailsAsync(string invitedEmailId, string invitedHnId, ContributorRole invitedRole) =>
            await _invitationsCollection.Find(invitation => invitation.InvitedEmail == invitedEmailId && invitation.InvitedHnId == invitedHnId && invitation.InvitedRoles.Contains(invitedRole) && invitation.Status == Enums.InvitationStatus.Accepted).FirstOrDefaultAsync();

        public async Task<Invitation> GetByInvitedEmailAsync(string invitedEmailId) =>
            await _invitationsCollection.Find(invitation => invitation.InvitedEmail == invitedEmailId).FirstOrDefaultAsync();

        public async Task<AcceptInvitationResult> AcceptAsync(InvitedUserRequest request)
        {
            var invitation = await GetByIdAsync(request.InvitationId);

            if (invitation == null)
                return AcceptInvitationResult.NotFound();

            // Mark accepted
            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;

            // If invitedRole is Network Manager
            //  - contributing org added
            //  - each rp's dashboard should reflect the nm
            //  - all hns of all invited orgId are added to hnMapping         
            // (taken care of in the same code as in else part)

            // Else

            // Retrieve invited user and the hnId that they are invited for
            var invitedUser = await _userService.GetByUserOneLoginIdAsync(request.OneLoginId);
            var heatNetwork = await _heatNetworkService.GetByHnIdAsync(invitation.InvitedHnId!);
            // HnId can be null in case of network manager, they will have invited orgId - check and fix

            // User exists
            if (invitedUser != null)
            {
                await UpdateExistingUser(invitedUser, invitation, heatNetwork);
                // update invitation after user is successfully updated
                await UpdateAsync(invitation.Id, invitation);
                return AcceptInvitationResult.Updated(invitedUser.Id!);
            }

            // New user created
            var newUser = await CreateUser(request, invitation, heatNetwork);
            // update invitation after user is successfully created
            await UpdateAsync(invitation.Id, invitation);
            return AcceptInvitationResult.Created(newUser.Id!);
        }

        public async Task<User> CreateUser(
        InvitedUserRequest request,
        Invitation invitation,
        HeatNetwork heatNetwork)
        {
            var user = await BuildUserFromInvitation(request, invitation);
            var userId = _userService.CreateAsync(user);

            await PostActions(invitation, user, heatNetwork);

            return user;
        }

        public async Task UpdateExistingUser(
        User user,
        Invitation invitation,
        HeatNetwork heatNetwork)
        {
            AddRoles(user, invitation);
            AddHnMapping(user, invitation);
            AddOrganisation(user, invitation);

            await _userService.UpdateAsync(user.Id!, user);

            await PostActions(invitation, user, heatNetwork);
        }

        public async void AddHnMapping(User user, Invitation invitation)
        {
            // Two cases to handle
            // If accepted as an NM - then all the hns that the inviter (RP - only possible option) owns will be mapped
            if (invitation.InvitedRoles.Contains(ContributorRole.NetworkManager))
            {
                var inviterRpDetails = await _userService.GetByIdAsync(invitation.InviterUserId);
                foreach (var hnRoleMapping in inviterRpDetails.HnRoleMappings)
                {
                    var existing = user.HnRoleMappings.FirstOrDefault(x => x.HnId == hnRoleMapping.HnId && x.Role == ContributorRole.NetworkManager);
                    if (existing == null)
                    {
                        user.HnRoleMappings.Add(new HnRoleMapping
                        {
                            HnId = hnRoleMapping.HnId,
                            Role = ContributorRole.NetworkManager
                        });
                    }
                }
            }// If accepted as anything else - then only add that one hn to that one role, if it doesn't already exist
            else
            {
                if (invitation.InvitedHnId == null)
                    return;

                user.HnRoleMappings ??= new List<HnRoleMapping>();

                foreach (var role in invitation.InvitedRoles)
                {
                    // Does mapping with same hnId and role already exist?
                    var existing = user.HnRoleMappings.FirstOrDefault(x => x.HnId == invitation.InvitedHnId && x.Role == role);
                    // If not
                    if (existing == null)
                    {
                        user.HnRoleMappings.Add(new HnRoleMapping
                        {
                            HnId = invitation.InvitedHnId,
                            Role = role
                        });
                    }
                }
            }
        }

        public void AddOrganisation(User user, Invitation invitation)
        {
            // Error!
            if (invitation.InvitedOrgId == null)
                return;

            user.ContributingOrganisations ??= new List<string>();

            if (!user.ContributingOrganisations.Contains(invitation.InvitedOrgId))
            {
                user.ContributingOrganisations.Add(invitation.InvitedOrgId);
            }
        }

        public void AddRoles(User user, Invitation invitation)
        {
            if (invitation.InvitedRoles == null)
                return;

            user.Roles ??= new List<UserRole>();
            var invitedRoles = MapAndFilterRoles(invitation.InvitedRoles);
            // If the user doesn't already have the role, add it
            foreach (var role in invitedRoles)
            {
                if (!user.Roles.Contains(role))
                    user.Roles.Add(role);
            }
        }


        public async Task PostActions(Invitation invitation, User user, HeatNetwork heatNetwork)
        {
            await AuditLogs(invitation, user.Id!, heatNetwork);
            await NotificationHistoryForAcceptingInvite(invitation, user, heatNetwork);
        }

        public async Task<User> BuildUserFromInvitation(InvitedUserRequest request, Invitation invitation)
        {
            var user = new User
            {
                OneLoginId = request.OneLoginId,
                EmailId = invitation.InvitedEmail,
                FirstName = invitation.FirstName,
                LastName = invitation.LastName,
                JobTitle = null,
                Status = UserStatus.Active,
                ContributingOrganisations = new List<string> { invitation.InvitedOrgId }
            };

            // In case of all roles except Network Manager
            if (invitation.InvitedHnId != null)
            {
                user.HnRoleMappings = new List<HnRoleMapping>
                {
                    new HnRoleMapping
                    {
                        HnId = invitation.InvitedHnId,
                        Role = invitation.InvitedRoles.FirstOrDefault()
                    }
                };
            }
            else
            {
                AddHnMapping(user, invitation);
            }

            user.Roles = MapAndFilterRoles(invitation.InvitedRoles);

            return user;
        }

        public async Task<List<Invitation>> GetNetworkManagersByInviterUserId(string userId)
        {
            // draw invitations by GetByInviterUserIdAsync
            var invitations = await GetByInviterUserIdAsync(userId);
            // filter out network managers
            var networkManagerInvitationEmails = invitations
                .Where(i => i.InvitedRoles != null && i.InvitedRoles.Contains(ContributorRole.NetworkManager))
                .Select(i => i.InvitedEmail).Distinct().ToList();
            var networkManagerInvitations = invitations
            .Where(i => networkManagerInvitationEmails.Contains(i.InvitedEmail))
            .ToList();
            return networkManagerInvitations;
        }

        public static readonly Dictionary<ContributorRole, UserRole> RoleMapping =
        new Dictionary<ContributorRole, UserRole>
        {
            { ContributorRole.DesignatedDutyHolder, UserRole.DesignatedDutyHolder },
            { ContributorRole.Contributor, UserRole.Contributor },
            { ContributorRole.Assessor, UserRole.Assessor },
            { ContributorRole.Certifier, UserRole.Certifier },
            { ContributorRole.NetworkManager, UserRole.NetworkManager },
            { ContributorRole.ResponsibleParty, UserRole.ResponsibleParty }
        };


        public List<UserRole> MapAndFilterRoles(List<ContributorRole>? rolesToMap)
        {
            return rolesToMap?
                .Select(role =>
                    RoleMapping.TryGetValue(role, out var mappedRole)
                    ? (UserRole?)mappedRole
                    : null
                )
                .Where(mappedRole => mappedRole.HasValue)
                .Select(mappedRole => mappedRole!.Value)
                .ToList()
                ?? new List<UserRole>();
        }
        public async Task AuditLogs(Invitation invitation, string userId, HeatNetwork heatNetwork)
        {
            // Log for Audit history
            var isRegistrationEnabledString = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLED");
            if (!string.IsNullOrEmpty(isRegistrationEnabledString) &&
                    isRegistrationEnabledString.ToLower() == "true")

            {
                if (heatNetwork != null)
                {
                    var phase = heatNetwork.Phase;
                    var stage = HeatNetworkHelper.GetStageFromPhase(phase);
                    var invitedRole = invitation.InvitedRoles.FirstOrDefault();
                    var entryType = "";
                    switch (invitedRole)
                    {
                        case ContributorRole.DesignatedDutyHolder:
                            entryType = "Designated duty holder assigned";
                            break;
                        case ContributorRole.Contributor:
                            entryType = "Contributor assigned";
                            break;
                        default:
                            break;
                    }
                    await _auditService.SaveAuditAsync<HeatNetwork>(
                        entryType: entryType,
                        actorId: userId,
                        entityId: heatNetwork.HnId!,
                        oldState: heatNetwork,
                        newState: heatNetwork,
                        elementName: "NA",
                        phase: phase,
                        stage: stage
                    );
                }
            }
        }

        public async Task NotificationHistoryForAcceptingInvite(Invitation invitation, User user, HeatNetwork heatNetwork)
        {
            var invitedRole = invitation.InvitedRoles.FirstOrDefault();
            var eligibleRoles = new List<string>() { ContributorRole.ResponsibleParty.ToString() };
            var subject = string.Empty;
            var action = string.Empty;
            var description = string.Empty;
            var notificationType = NotificationHistoryType.NA;
            var actorIds = new List<string>() { invitation.InviterUserId };
            var heatNetworkName = heatNetwork != null ? heatNetwork.Name : "";

            if (invitedRole == ContributorRole.NetworkManager)
            {
                subject = NotificationHistorySubjects.NetworkManagerJoined;
                action = NotificationHistoryActions.NetworkManagers;
                description = $"{user.FirstName} {user.LastName} signed in";
                notificationType = NotificationHistoryType.NetworkManagerAcceptsInvite;
            }
            else if (invitedRole == ContributorRole.DesignatedDutyHolder)
            {
                // check if the invitor is Network Manager, if yes then add RP to actorIds
                var invitorDetailsOfNM = await _userService.GetByIdAsync(invitation.InviterUserId);
                if (invitorDetailsOfNM != null && invitorDetailsOfNM.Roles.Contains(UserRole.NetworkManager))
                {
                    // Get the RP user details and add to actorIds
                    var invitaions = await GetByInvitedEmailAsync(invitorDetailsOfNM.EmailId!);
                    if (invitaions != null)
                        actorIds.AddRange(invitaions.InviterUserId);
                }

                eligibleRoles.Add(ContributorRole.NetworkManager.ToString());
                subject = NotificationHistorySubjects.DesignatedDutyHolderJoined;
                action = NotificationHistoryActions.DDHAndContributors;
                description = $"{user.FirstName} {user.LastName} joined {invitation.InvitedHnId}-{heatNetworkName}";
                notificationType = NotificationHistoryType.DdhAcceptsInviteToHeatNetwork;
            }
            else if (invitedRole == ContributorRole.Contributor)
            {
                await AddAssociatedNetworkManagerAndRpIds(invitation, actorIds);
                eligibleRoles.Add(ContributorRole.NetworkManager.ToString());
                eligibleRoles.Add(ContributorRole.DesignatedDutyHolder.ToString());
                subject = NotificationHistorySubjects.ContributorJoined;
                action = NotificationHistoryActions.DDHAndContributors;
                description = $"{user.FirstName} {user.LastName} joined {invitation.InvitedHnId}-{heatNetworkName}";
                notificationType = NotificationHistoryType.ContributorAcceptsInviteToHeatNetwork;
            }

            var notificationHistory = new NotificationHistory
            {
                NotificationType = notificationType,
                ActorsId = actorIds,
                Subject = subject,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Action = action,
                EligibleRoles = eligibleRoles,
                HeatNetworkId = invitation.InvitedHnId,
                CreatedBy = user.Id
            };

            await _notificationHistoryService.CreateAsync(notificationHistory);
        }

        public async Task AddAssociatedNetworkManagerAndRpIds(Invitation invitation, List<string> actorIds)
        {
            var invitorDetails = await _userService.GetByIdAsync(invitation.InviterUserId);
            if (invitorDetails == null) return;

            var role = invitorDetails.HnRoleMappings
                .Where(mapping => mapping.HnId == invitation.InvitedHnId)
                .Select(mapping => mapping.Role)
                .FirstOrDefault();


            if (role == ContributorRole.NetworkManager)
            {
                var invitaions = await GetByInvitedEmailAsync(invitorDetails.EmailId!);
                if (invitaions != null)
                    actorIds.AddRange(invitaions.InviterUserId);
            }
            else
            {
                var superInvitorDetails = await GetByInvitedDetailsAsync(
                    invitorDetails.EmailId,
                    invitation.InvitedHnId!,
                    role);

                if (superInvitorDetails != null)
                {
                    actorIds.Add(superInvitorDetails.InviterUserId!);
                    // check if the invitor is Network Manager, if yes then add RP to actorIds
                    var invitorDetailsOfNM = await _userService.GetByIdAsync(superInvitorDetails!.InviterUserId);
                    if (invitorDetailsOfNM != null && invitorDetailsOfNM.Roles.Contains(UserRole.NetworkManager))
                    {
                        // Get the RP user details and add to actorIds
                        var invitaions = await GetByInvitedEmailAsync(invitorDetailsOfNM.EmailId!);
                        if (invitaions != null)
                            actorIds.AddRange(invitaions.InviterUserId);
                    }
                }
            }

        }
    }
}

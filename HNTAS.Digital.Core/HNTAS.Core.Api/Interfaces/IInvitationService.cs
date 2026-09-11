using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IInvitationService
    {
        Task<List<Invitation>> GetAsync();
        Task<Invitation> GetByIdAsync(string id);
        Task<Invitation> GetByEmailAsync(string invitedEmail, string hnId);
        Task<List<Invitation>> GetByInviterUserIdAsync(string inviterUserId);
        Task CreateAsync(Invitation newInvitation);
        Task UpdateAsync(string id, Invitation updatedInvitation);
        //Task RemoveAsync(string id);
        Task<List<ManagedUserResponse>> GetInvitedUsersAsRegisteredAsync(string inviterUserId);
        Task<List<Invitation>> GetNetworkManagersByInviterUserId(string userId);
        Task<AcceptInvitationResult> AcceptAsync(InvitedUserRequest request);
        Task<User> CreateUser(InvitedUserRequest request, Invitation invitation,HeatNetwork heatNetwork);
        Task UpdateExistingUser(User user, Invitation invitation, HeatNetwork heatNetwork);
        void AddHnMapping(User user, Invitation invitation);
        void AddOrganisation(User user, Invitation invitation);
        void AddRoles(User user, Invitation invitation);
        Task PostActions(Invitation invitation, User user, HeatNetwork heatNetwork);
        Task<User> BuildUserFromInvitation(InvitedUserRequest request, Invitation invitation);
        List<UserRole> MapAndFilterRoles(List<ContributorRole>? rolesToMap);
        Task AuditLogs(Invitation invitation, string userId, HeatNetwork heatNetwork);
        Task NotificationHistoryForAcceptingInvite(Invitation invitation, User user, HeatNetwork heatNetwork);
        Task AddAssociatedNetworkManagerAndRpIds(Invitation invitation, List<string> actorIds);
        Task<Invitation> GetByInvitedDetailsAsync(string invitedEmailId, string invitedHnId, ContributorRole invitedRole);
        Task<List<Invitation>> GetByEmailsAndHnIdAsync(List<string> invitedEmails, string hnId);
        Task<Invitation> GetByInvitedEmailAsync(string invitedEmailId);
        Task<(List<ManagedUserResponse> items, long totalCount)> GetInvitedUsersDdhAndContributorsAsync(string inviterUserId, int pageNumber, int pageSize, string sortBy, string sortDirection);
    }
}

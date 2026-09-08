using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Models;
using MongoDB.Driver;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAsync();
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUserOneLoginIdAsync(string userId);
        Task<User?> GetByEmailAsync(string emailId);
        Task<List<User>> GetUsersByOrgIdAsync(string organisationId);
        Task CreateAsync(User newUser);
        Task UpdateAsync(string id, User updatedUser);
        Task<UpdateResult> UpdateOrgIdAsync(string userId, string orgId);
        Task RemoveAsync(string id);
        Task<List<User>> GetRegisteredUsers(List<string> invitedEmails);
        Task<UserDetailsResult> GetUserWithDetailsAsync(string userId);

        Task<List<User>> GetAssessorsByHnIdAsync(string hnId);
        Task<User?> GetResponsiblePartyByHnIdAsync(string hnId);
        Task<List<User>> GetContributorsByHnIdAsync(string hnId);

        Task<List<UserDetailsResult>> GetUsersByInvitedEmailsWithDetailsAsync(List<string> invitedEmails);
        Task<List<UserRoleDetailResponse>> GetHeatNetworkUsersWithRolesAsync(string hnId);
        Task<List<User>> GetUsersAssociatedByHnIdAsync(string hnId);
        Task UpdateUserNetwork(string userId, string hnId, ContributorRole role = ContributorRole.ResponsibleParty);
        Task<List<User>> GetActiveNetworkManagersByRpUserIdAsync(string rpUserId);
    }
}

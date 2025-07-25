using HNTAS.Core.Api.Models;

namespace HNTAS.Core.Api.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAsync();
        Task<User> GetByIdAsync(string id);
        Task<User> GetByUserIdAsync(string userId);
        Task CreateAsync(User newUser);
        Task UpdateAsync(string id, User updatedUser);
        Task RemoveAsync(string id);
    }
}

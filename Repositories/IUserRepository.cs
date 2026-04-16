using StockMaster.Models;

namespace StockMaster.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> ValidateUserAsync(string username, string password);
        Task UpdateLastLoginAsync(int userId);
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
        Task<User?> GetUserWithDetailsAsync(int id);
    }
}
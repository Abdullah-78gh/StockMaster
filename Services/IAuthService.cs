using StockMaster.Models;
using StockMaster.ViewModels;

namespace StockMaster.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User> RegisterAsync(RegisterViewModel model);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task LogoutAsync();
        User? GetCurrentUser();
        bool IsAuthenticated();
        bool IsInRole(UserRole role);
        Task<bool> UpdateProfileImageAsync(int userId, string imageUrl);
    }
}
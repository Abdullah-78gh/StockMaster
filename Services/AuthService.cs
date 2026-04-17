using Microsoft.AspNetCore.Http;
using StockMaster.Models;
using StockMaster.Repositories;
using StockMaster.ViewModels;
using System.Text.Json;

namespace StockMaster.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKeyUser = "CurrentUser";

        public AuthService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var isValid = await _userRepository.ValidateUserAsync(username, password);
            if (!isValid)
                return null;

            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null)
                return null;

            await _userRepository.UpdateLastLoginAsync(user.Id);

            // Store user in session
            var userJson = JsonSerializer.Serialize(user);
            _httpContextAccessor.HttpContext?.Session.SetString(SessionKeyUser, userJson);

            return user;
        }

        public async Task<User> RegisterAsync(RegisterViewModel model)
        {
            // Check if username already exists
            var existingUser = await _userRepository.GetUserByUsernameAsync(model.Username);
            if (existingUser != null)
                throw new InvalidOperationException("Username already exists");

            // Check if email already exists
            existingUser = await _userRepository.GetUserByEmailAsync(model.Email);
            if (existingUser != null)
                throw new InvalidOperationException("Email already exists");

            var user = new User
            {
                Username = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return user;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public Task LogoutAsync()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(SessionKeyUser);
            return Task.CompletedTask;
        }

        public User? GetCurrentUser()
        {
            var userJson = _httpContextAccessor.HttpContext?.Session.GetString(SessionKeyUser);
            if (string.IsNullOrEmpty(userJson))
                return null;

            return JsonSerializer.Deserialize<User>(userJson);
        }

        public bool IsAuthenticated()
        {
            return GetCurrentUser() != null;
        }

        public bool IsInRole(UserRole role)
        {
            var user = GetCurrentUser();
            return user != null && user.Role == role;
        }

        public async Task<bool> UpdateProfileImageAsync(int userId, string imageUrl)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.ProfileImageUrl = imageUrl;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Update user in session
            var userJson = JsonSerializer.Serialize(user);
            _httpContextAccessor.HttpContext?.Session.SetString(SessionKeyUser, userJson);

            return true;
        }
    }
}
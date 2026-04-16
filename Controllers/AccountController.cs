using Microsoft.AspNetCore.Mvc;
using StockMaster.Services;
using StockMaster.ViewModels;
using StockMaster.Models;

namespace StockMaster.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If user is already logged in, redirect to dashboard
            if (_authService.IsAuthenticated())
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _authService.AuthenticateAsync(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account is deactivated. Please contact administrator.");
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            // Only Admin can register new users
            if (!_authService.IsInRole(UserRole.Admin))
                return RedirectToAction("AccessDenied");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!_authService.IsInRole(UserRole.Admin))
                return RedirectToAction("AccessDenied");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _authService.RegisterAsync(model);
                TempData["Success"] = $"User {user.Username} registered successfully!";
                return RedirectToAction("Index", "Home");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var user = _authService.GetCurrentUser();
            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = _authService.GetCurrentUser();
            if (user == null)
                return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "New password and confirm password do not match";
                return RedirectToAction("Profile");
            }

            var result = await _authService.ChangePasswordAsync(user.Id, currentPassword, newPassword);
            if (result)
            {
                TempData["Success"] = "Password changed successfully";
            }
            else
            {
                TempData["Error"] = "Current password is incorrect";
            }

            return RedirectToAction("Profile");
        }
    }
}
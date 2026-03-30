using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolyCafeMenuWeb.Data;
using PolyCafeMenuWeb.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PolyCafeMenuWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly PolyCafeContext _context;
        private readonly IEmailService _emailService;

        public AuthController(PolyCafeContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectBasedOnRole();
            }

            ViewBag.Error = TempData["Error"];
            ViewBag.Success = TempData["Success"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Username == username && e.Password == password && e.IsActive);

            if (employee == null)
            {
                ViewBag.Error = "Invalid username or password!";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.EmployeeID.ToString()),
                new Claim(ClaimTypes.Name, employee.FullName),
                new Claim(ClaimTypes.Role, employee.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Set session for easy access
            HttpContext.Session.SetInt32("EmployeeID", employee.EmployeeID);
            HttpContext.Session.SetString("FullName", employee.FullName);
            HttpContext.Session.SetString("Role", employee.Role);
            HttpContext.Session.SetString("Username", employee.Username);

            return RedirectBasedOnRole(employee.Role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string username, string email)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Vui long nhap day du ten dang nhap va email nhan vien.";
                return RedirectToAction(nameof(Login));
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e =>
                e.Username == username &&
                e.Email == email &&
                e.IsActive);

            if (employee == null)
            {
                TempData["Error"] = "Khong tim thay nhan vien phu hop voi thong tin da cung cap.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var newPassword = GenerateRandomPassword();
                await using var transaction = await _context.Database.BeginTransactionAsync();

                employee.Password = newPassword;
                await _context.SaveChangesAsync();
                await _emailService.SendPasswordResetEmailAsync(employee.Email, employee.FullName, employee.Username, newPassword);
                await transaction.CommitAsync();
                TempData["Success"] = "Mat khau moi da duoc gui den email nhan vien.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Khong the gui email cap lai mat khau: {ex.Message}";
            }

            return RedirectToAction(nameof(Login));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectBasedOnRole(string? role = null)
        {
            role ??= User.FindFirst(ClaimTypes.Role)?.Value;

            switch (role)
            {
                case "Manager":
                    return RedirectToAction("Dashboard", "Manager");
                case "Cashier":
                    return RedirectToAction("Index", "POS");
                case "Barista":
                    return RedirectToAction("Queue", "Barista");
                default:
                    return RedirectToAction("Login");
            }
        }

        private static string GenerateRandomPassword(int length = 10)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$%";
            var bytes = RandomNumberGenerator.GetBytes(length);
            var passwordChars = new char[length];

            for (var i = 0; i < length; i++)
            {
                passwordChars[i] = chars[bytes[i] % chars.Length];
            }

            return new string(passwordChars);
        }
    }
}

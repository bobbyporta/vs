using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PUBReservationSystem.Models;



namespace PUBReservationSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Login()
        {
            if (TempData["Error"] != null)
                ViewBag.Error = TempData["Error"].ToString();
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> DoLogin(string username, string password)
        {
            try
            {
                var user = _context.Users
                    .Include(u => u.Employee)
                    .FirstOrDefault(u => u.Username == username);

                if (user == null)
                    return Json(new { success = false, userNotFound = true, message = "User not found" });

                if (user.Account_Locked)
                    return Json(new { success = false, isLocked = true, message = "Account is locked." });

                if (!user.Is_Active)
                    return Json(new { success = false, message = "Account is deactivated." });

                // 🔐 1. INITIALIZE HASHER 
                var hasher = new PasswordHasher<Users>();
                var verificationResult = hasher.VerifyHashedPassword(user, user.Password_Hash ?? "", password);

                bool loginSuccessful = false;
                bool upgradeToHash = false;

                // 2. EVALUATE SECURITY THRESHOLDS
                if (verificationResult == PasswordVerificationResult.Success)
                {
                    loginSuccessful = true;
                }
                // 🚀 SMART FALLBACK: If hash matching fails, check if the DB still has the old plaintext version!
                else if (user.Password_Hash == password)
                {
                    loginSuccessful = true;
                    upgradeToHash = true; // Flag this user record for an immediate cryptographic upgrade
                }

                if (loginSuccessful)
                {
                    // Reset failed login tracking tokens
                    user.Login_Attempts = 0;
                    user.Account_Locked = false;
                    user.Last_Login = DateTime.Now;

                    // If they logged in via legacy plain-text, convert them to a secure hash right now!
                    if (upgradeToHash)
                    {
                        user.Password_Hash = hasher.HashPassword(user, password);
                    }

                    _context.SaveChanges();

                    // Track terminal execution metrics
                    _context.AuditLog.Add(new AuditLog
                    {
                        User_ID = user.User_ID,
                        Action = "LOGIN",
                        Description = upgradeToHash
                            ? "User logged in successfully (Account upgraded to Secure Hashing)."
                            : "User logged in successfully.",
                        Timestamp = DateTime.Now
                    });
                    _context.SaveChanges();

                    // Establish foundational security flags across session states
                    HttpContext.Session.SetString("User_ID", user.User_ID.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);
                    HttpContext.Session.SetString("FullName", user.Employee?.Full_Name ?? user.Username);

                    int branchId = user.Employee?.Branch_ID ?? 0;
                    HttpContext.Session.SetString("Branch_ID", branchId.ToString());

                    return Json(new { success = true });
                }
                else
                {
                    // 3. Handle Wrong Password State
                    user.Login_Attempts++;
                    int attemptsLeft = 3 - user.Login_Attempts;

                    if (user.Login_Attempts >= 3)
                    {
                        user.Account_Locked = true;
                        _context.SaveChanges();
                        return Json(new { success = false, isLocked = true, message = "Account locked." });
                    }

                    _context.SaveChanges();
                    return Json(new { success = false, attemptsLeft = attemptsLeft, message = $"Invalid password. {attemptsLeft} attempts remaining." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"ERROR: {ex.Message} | {ex.InnerException?.Message}" });
            }
        }

        public async Task<IActionResult> Logout()
        {
            var userIdString = HttpContext.Session.GetString("User_ID");

            // Safely parse the ID without risking a crash
            if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
            {
                _context.AuditLog.Add(new AuditLog
                {
                    User_ID = userId,
                    Action = "LOGOUT",
                    Description = "User logged out",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            // Completely wipes User_ID, Role, Branch_ID, etc.
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}
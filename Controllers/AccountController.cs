
using FinTrack_Pro.Data;
using FinTrack_Pro.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinTrack_Pro.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor: Database connection setup
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. REGISTRATION (SIGN UP)
        // ==========================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                // Check karein ke email pehle se toh nahi hai?
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    ViewBag.Error = "Yeh Email pehle se registered hai!";
                    return View(user);
                }

                // Database mein naya user save karein
                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }
            return View(user);
        }

        // ==========================================
        // 2. LOGIN
        // ==========================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string passwordHash)
        {
            // Database mein user verify karein
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == passwordHash);

            if (user != null)
            {
                // Cookie Authentication ke liye Claims (Virtual ID Card) banayen
                // (Yahan humne 'user.UserId' ki jagah 'user.Id' use kiya hai jo error fix karega)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // User ko login karwa dein (Cookie save karein)
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                HttpContext.Session.SetInt32("UserId", user.Id);

                // Login ke baad Home page par bhejein
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid Email or Password!";
            return View();
        }

        // ==========================================
        // 3. LOGOUT
        // ==========================================

        public async Task<IActionResult> Logout()
        {
            // User ka session (Cookie) khatam karein
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // ==========================================
        // 4. GOOGLE OAUTH LOGIN
        // ==========================================

        [HttpPost]
        public IActionResult GoogleLogin()
        {
            // Google auth trigger karein aur wapas "GoogleResponse" method par aayen
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            // Google se wapas aane wala data (Identity) read karein
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return RedirectToAction("Login");

            // User ka Email aur Name nikalen
            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (email == null)
            {
                ViewBag.Error = "Google Authentication Failed.";
                return View("Login");
            }

            // Database mein check karein ke user pehle se hai ya nahi
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                // Naya User banayen (Kyunke Google wale ka password nahi hota, null chhor dein)
                user = new User
                {
                    Email = email,
                    FullName = name,
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            // User ko apni App mein Login karwa dein (Aapka apna logic)
            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? "User"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            HttpContext.Session.SetInt32("UserId", user.Id);

            return RedirectToAction("Index", "Home");
        }
    }
}
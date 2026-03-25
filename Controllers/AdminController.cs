using FinTrack_Pro.Data;
using FinTrack_Pro.Models; // YEH LINE ADD KAREIN
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; // Yeh add karein
using Microsoft.AspNetCore.Http; // Yeh add karein
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace FinTrack_Pro.Controllers
{
    // YEH LINE SABSE ZAROORI HAI: Sirf wo log aayenge jinki Cookie mein Role "Admin" hoga
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // Naya addition

        // Constructor ko update karein
        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // 1. ADMIN DASHBOARD (Main Screen)
        // ==========================================
        public IActionResult Index()
        {
            // Dashboard ke liye hum database se thodi calculation kar ke bhejenge
            ViewBag.TotalUsers = _context.Users.Count(u => u.Role == "User");
            ViewBag.TotalAdmins = _context.Users.Count(u => u.Role == "Admin");

            // Aakhri 5 naye aane wale users ki list
            var recentUsers = _context.Users
                                      .OrderByDescending(u => u.CreatedAt)
                                      .Take(5)
                                      .ToList();

            return View(recentUsers);
        }

        // ==========================================
        // 2. ALL USERS LIST
        // ==========================================
        public IActionResult UsersList()
        {
            // Database se saare users utha kar view mein bhejain
            var allUsers = _context.Users.OrderByDescending(u => u.CreatedAt).ToList();
            return View(allUsers);
        }

        // ==========================================
        // 3. EDIT USER (GET & POST)
        // ==========================================
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public IActionResult EditUser(int id, string fullName, string role)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.FullName = fullName;
                user.Role = role; // Admin yahan se kisi ko bhi Admin bana sakta hai
                _context.SaveChanges();

                return RedirectToAction("UsersList");
            }
            return View(user);
        }

        // ==========================================
        // 4. DELETE USER
        // ==========================================
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                // Agar chaho toh yahan check laga sakte ho ke Admin khud ko delete na kar le
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
            return RedirectToAction("UsersList");
        }

        // ==========================================
        // 5. SYSTEM ANALYTICS
        // ==========================================
        public IActionResult Analytics()
        {
            // Basic Counts
            ViewBag.AdminCount = _context.Users.Count(u => u.Role == "Admin");
            ViewBag.UserCount = _context.Users.Count(u => u.Role == "User");
            ViewBag.TotalUsers = _context.Users.Count();

            // Puraane mahinon ka data (Chart ke liye)
            // Hum is saal ke naye users ko month ke hisaab se group kar rahe hain
            var currentYear = System.DateTime.Now.Year;
            var usersThisYear = _context.Users
                                        .Where(u => u.CreatedAt.Year == currentYear)
                                        .ToList();

            int[] monthlyRegistrations = new int[12];
            foreach (var user in usersThisYear)
            {
                // Month 1-12 hota hai, array index 0-11 hota hai
                monthlyRegistrations[user.CreatedAt.Month - 1]++;
            }

            // Array ko comma-separated string bana kar View mein bhejenge (Chart.js ke liye)
            ViewBag.MonthlyData = string.Join(",", monthlyRegistrations);

            return View();
        }

        // ==========================================
        // 6. ADMIN SETTINGS (GET & POST)
        // ==========================================
        [HttpGet]
        public IActionResult Settings()
        {
            // Hum assume kar rahe hain ke logged-in user ki email User.Identity.Name mein hai
            // (Aap isay apne hisaab se id ya email se change kar sakte hain)
            var userEmail = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            if (user == null)
            {
                // Agar authentication claims alag hain, toh pehla admin utha lein testing ke liye
                user = _context.Users.FirstOrDefault(u => u.Role == "Admin");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(User updatedUser, IFormFile ProfileImage, IFormFile BannerImage)
        {
            var user = _context.Users.Find(updatedUser.Id);
            if (user == null) return NotFound();

            // Text fields update
            user.FullName = updatedUser.FullName;
            user.Bio = updatedUser.Bio;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Profession = updatedUser.Profession;

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Profile Image Upload Handle
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + ProfileImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(fileStream);
                }
                user.ProfilePicturePath = "/uploads/" + uniqueFileName;
            }

            // Banner Upload Handle
            if (BannerImage != null && BannerImage.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + BannerImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await BannerImage.CopyToAsync(fileStream);
                }
                user.BannerPath = "/uploads/" + uniqueFileName;
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "System Profile Successfully Updated.";
            return RedirectToAction("Settings");
        }
    }
}

   
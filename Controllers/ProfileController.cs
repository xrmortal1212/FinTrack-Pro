using FinTrack_Pro.Data;
using FinTrack_Pro.Models; // Apne namespace ke hisaab se adjust karein
using Microsoft.AspNetCore.Hosting; // IWebHostEnvironment ke liye
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FinTrack_Pro.Controllers
{
    public class ProfileController : Controller
    {
        // Yahan 'ApplicationDbContext' ki jagah apne database context ka asal naam likhein (e.g., AppDbContext)
        private readonly ApplicationDbContext _context;

        // IWebHostEnvironment humein wwwroot folder ka rasta (path) nikalne mein madad karta hai
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ---------------------------------------------------
        // 1. Profile Dashboard Dekhne ke liye
        // ---------------------------------------------------
        public IActionResult Index()
        {
            // Assume kar rahe hain ke Login ke baad aapne UserId Session mein save kiya hai
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account"); // Agar login nahi hai toh wapis bhej dein
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            return View(user);
        }

        // ---------------------------------------------------
        // 2. Edit Profile ka Form (Page Load karne ke liye)
        // ---------------------------------------------------
        [HttpGet]
        public IActionResult Edit()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            return View(user);
        }

        // ---------------------------------------------------
        // 3. Edit Profile (Data & Images Save karne ke liye)
        // ---------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Edit(User updatedUser, IFormFile? profilePicture, IFormFile? bannerImage)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            // Sirf wahi details update karein jo form se aayi hain
            user.FullName = updatedUser.FullName;
            user.Bio = updatedUser.Bio;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Profession = updatedUser.Profession;

            // Woh folder path jahan hume images save karni hain: wwwroot/uploads/profiles
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");

            // Agar yeh folder exist nahi karta, toh naya bana do
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // --- Profile Picture Save Logic ---
            if (profilePicture != null && profilePicture.Length > 0)
            {
                // Unique naam de rahe hain taake agar 2 users same naam ki pic upload karein toh overwrite na ho
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }
                // Database mein sirf image ka rasta (path) save karenge
                user.ProfilePicturePath = "/uploads/profiles/" + uniqueFileName;
            }

            // --- Banner Image Save Logic ---
            if (bannerImage != null && bannerImage.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + "_banner_" + bannerImage.FileName;
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await bannerImage.CopyToAsync(fileStream);
                }
                user.BannerPath = "/uploads/profiles/" + uniqueFileName;
            }

            // Database ko update karein
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Success message bhej dein
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }
    }
}
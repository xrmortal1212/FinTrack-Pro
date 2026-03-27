using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinTrack_Pro.Data;
using FinTrack_Pro.Models;
using System.Security.Claims;
using System.Linq;

namespace FinTrack_Pro.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // HELPER METHOD (Suffix Add/Update Karne Ke Liye)
        // ==========================================
        private string AppendTypeSuffix(string categoryTitle, string categoryType)
        {
            if (string.IsNullOrEmpty(categoryTitle) || string.IsNullOrEmpty(categoryType))
                return categoryTitle;

            // 1. Pehle purane tags hata do taake agar koi Edit kare toh double tag na lag jaye
            string cleanTitle = categoryTitle.Replace(" - Expense", "").Replace(" - Income", "").Trim();

            // 2. Ab Type ke hisaab se naya tag laga do
            if (categoryType.Equals("Expense", System.StringComparison.OrdinalIgnoreCase))
            {
                return cleanTitle + " - Expense";
            }
            else if (categoryType.Equals("Income", System.StringComparison.OrdinalIgnoreCase))
            {
                return cleanTitle + " - Income";
            }

            return cleanTitle;
        }

        // ==========================================
        // 1. INDEX - (LIST OF CATEGORIES & STATS)
        // ==========================================
        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var categories = _context.Categories.Where(c => c.UserId == userId).ToList();

            // SUMMARY STATS
            ViewBag.TotalCategories = categories.Count;
            ViewBag.IncomeCount = categories.Count(c => c.Type.Equals("Income", StringComparison.OrdinalIgnoreCase));
            ViewBag.ExpenseCount = categories.Count(c => c.Type.Equals("Expense", StringComparison.OrdinalIgnoreCase));

            return View(categories);
        }

        // ==========================================
        // 2. CREATE CATEGORY
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            category.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                // Jese hi save hone lage, Title ke aage Suffix laga do
                category.Title = AppendTypeSuffix(category.Title, category.Type);

                _context.Categories.Add(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(category);
        }

        // ==========================================
        // 3. EDIT (UPDATE) CATEGORY
        // ==========================================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id && c.UserId == userId);

            if (category == null) return NotFound();

            // Suffix hata kar Edit box mein dikhao (taake user ko - Expense likha hua na mile edit karte waqt)
            if (!string.IsNullOrEmpty(category.Title))
            {
                category.Title = category.Title.Replace(" - Expense", "").Replace(" - Income", "").Trim();
            }

            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            category.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                // Update hone se pehle dobara tag update kar do
                category.Title = AppendTypeSuffix(category.Title, category.Type);

                _context.Categories.Update(category);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(category);
        }

        // ==========================================
        // 4. DELETE CATEGORY (Yeh Confirmation Page Dikhayega)
        // ==========================================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id && c.UserId == userId);

            if (category == null) return NotFound();

            // SAFE DELETE CHECK: Agar transactions hain toh delete page par jane hi na do
            bool hasTransactions = _context.Transactions.Any(t => t.CategoryId == id && t.UserId == userId);
            if (hasTransactions)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{category.Title}' because it has transactions linked to it.";
                return RedirectToAction("Index");
            }

            return View(category); // Delete.cshtml ko open karega
        }

        // ==========================================
        // 5. DELETE CONFIRMED (Yeh Asal Mein Delete Karega)
        // ==========================================
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id && c.UserId == userId);

            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Category deleted successfully!";
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 5. ONE-TIME FIX: Purani Categories ko Update Karne Ke Liye
        // ==========================================
        [HttpGet]
        public IActionResult FixOldCategories()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // User ki saari categories database se nikal lo
            var categories = _context.Categories.Where(c => c.UserId == userId).ToList();

            foreach (var cat in categories)
            {
                // Agar title mein pehle se " - Expense" ya " - Income" nahi laga hua, toh lagao
                if (!cat.Title.EndsWith(" - Expense") && !cat.Title.EndsWith(" - Income"))
                {
                    if (cat.Type.Equals("Expense", System.StringComparison.OrdinalIgnoreCase))
                    {
                        cat.Title = cat.Title.Trim() + " - Expense";
                    }
                    else if (cat.Type.Equals("Income", System.StringComparison.OrdinalIgnoreCase))
                    {
                        cat.Title = cat.Title.Trim() + " - Income";
                    }
                }
            }

            // Database mein saari changes ek hi dafa save kar do
            _context.Categories.UpdateRange(categories);
            _context.SaveChanges();

            // Wapis categories ki list par bhej do
            return RedirectToAction("Index");
        }
    }
}
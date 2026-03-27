using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FinTrack_Pro.Data;
using FinTrack_Pro.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace FinTrack_Pro.Controllers
{
    [Authorize]
    public class BudgetController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BudgetController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. INDEX (Budgets, Progress aur Modal Data)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var budgets = await _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            ViewBag.Transactions = transactions;

            // NAYA: Modal ke dropdown ke liye categories Index par hi bhej rahe hain
            ViewBag.CategoryId = new SelectList(await _context.Categories
                .Where(c => c.UserId == userId && c.Type == "Expense")
                .ToListAsync(), "CategoryId", "Title");

            return View(budgets);
        }

        // ==========================================
        // 2. CREATE (Naya Budget Set Karna)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId && c.Type == "Expense"), "CategoryId", "Title");
            return View(new Budget { Month = DateTime.Now.Month, Year = DateTime.Now.Year });
        }

        [HttpPost]
        public IActionResult Create(Budget budget)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            budget.UserId = userId;

            // Modal se Month aur Year nahi aa raha toh current laga dein
            if (budget.Month == 0) budget.Month = DateTime.Now.Month;
            if (budget.Year == 0) budget.Year = DateTime.Now.Year;

            ModelState.Remove("User");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                var existing = _context.Budgets.FirstOrDefault(b => b.CategoryId == budget.CategoryId && b.Month == budget.Month && b.Year == budget.Year && b.UserId == userId);

                if (existing != null)
                {
                    TempData["ErrorMessage"] = "Is category ka budget is mahine ke liye pehle hi majood hai!";
                    return RedirectToAction("Index"); // Modal error ke baad wapish index par layein
                }

                _context.Budgets.Add(budget);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Budget successfully set!";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index"); // Agar invalid ho tab bhi index pe bhej dein
        }

        // ==========================================
        // 3. EDIT (Budget Update Karna)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.BudgetId == id && b.UserId == userId);

            if (budget == null) return NotFound();

            ViewBag.CategoryId = new SelectList(_context.Categories.Where(c => c.UserId == userId && c.Type == "Expense"), "CategoryId", "Title", budget.CategoryId);
            return View(budget);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Budget budget)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (id != budget.BudgetId) return NotFound();

            budget.UserId = userId;
            ModelState.Remove("User");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                _context.Update(budget);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Budget updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories.Where(c => c.UserId == userId && c.Type == "Expense"), "CategoryId", "Title", budget.CategoryId);
            return View(budget);
        }

        // ==========================================
        // 4. DELETE (Budget Remove Karna)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var budget = await _context.Budgets
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.BudgetId == id && b.UserId == userId);

            if (budget == null) return NotFound();

            return View(budget);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.BudgetId == id && b.UserId == userId);

            if (budget != null)
            {
                _context.Budgets.Remove(budget);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Budget deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
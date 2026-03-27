using Microsoft.AspNetCore.Mvc;
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
    public class GoalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GoalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. INDEX (Goals aur Stats)
        // ==========================================        
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var goals = await _context.Goals
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.Deadline)
                .ToListAsync();

            // Stats for the view
            ViewBag.TotalTarget = goals.Sum(g => g.TargetAmount);
            ViewBag.TotalSaved = goals.Sum(g => g.SavedAmount);
            ViewBag.TotalRemaining = ViewBag.TotalTarget - ViewBag.TotalSaved;

            return View(goals);
        }

        // ==========================================
        // 2. CREATE (Naya Goal Set Karna)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Goal { Deadline = DateTime.Now.AddMonths(1) });
        }

        [HttpPost]
        public IActionResult Create(Goal goal)
        {
            goal.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.Goals.Add(goal);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(goal);
        }

        // ==========================================
        // 3. ADD FUNDS (Goal mein paise jama karna)
        // ==========================================
        [HttpPost]
        public IActionResult AddFunds(int GoalId, decimal AmountToAdd)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var goal = _context.Goals.FirstOrDefault(g => g.GoalId == GoalId && g.UserId == userId);

            if (goal != null && AmountToAdd > 0)
            {
                goal.SavedAmount += AmountToAdd;

                if (goal.SavedAmount > goal.TargetAmount)
                {
                    goal.SavedAmount = goal.TargetAmount;
                }

                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. DELETE GOAL (NEW)
        // ==========================================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var goal = _context.Goals.FirstOrDefault(g => g.GoalId == id && g.UserId == userId);

            if (goal != null)
            {
                _context.Goals.Remove(goal);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Goal deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}

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
    public class InvestmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvestmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. INDEX (List & Portfolio Stats)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var investments = await _context.Investments
                .Where(i => i.UserId == userId)
                .ToListAsync();

            // Stats calculate karna
            ViewBag.TotalInvested = investments.Sum(i => i.InvestedAmount);
            ViewBag.TotalCurrentValue = investments.Sum(i => i.CurrentValue);
            ViewBag.TotalProfit = ViewBag.TotalCurrentValue - ViewBag.TotalInvested;

            return View(investments);
        }

        // ==========================================
        // 2. CREATE (New Investment)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            // Dropdown ke liye categories
            ViewBag.Categories = new List<string> { "Stocks", "Crypto", "Gold", "Real Estate", "Mutual Funds", "Other" };
            return View();
        }

        [HttpPost]
        public IActionResult Create(Investment investment)
        {
            investment.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                investment.CurrentValue = investment.InvestedAmount;

                _context.Investments.Add(investment);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Investment added successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new List<string> { "Stocks", "Crypto", "Gold", "Real Estate", "Mutual Funds", "Other" };
            return View(investment);
        }

        // ==========================================
        // 3. UPDATE VALUE (Manual Live Rate)
        // ==========================================
        [HttpPost]
        public IActionResult UpdateValue(int InvestmentId, decimal NewValue)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var investment = _context.Investments.FirstOrDefault(i => i.InvestmentId == InvestmentId && i.UserId == userId);

            if (investment != null && NewValue >= 0)
            {
                investment.CurrentValue = NewValue;
                _context.SaveChanges();
                TempData["SuccessMessage"] = $"{investment.AssetName} value updated to Rs. {NewValue}!";
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. DELETE (Sell or Close)
        // ==========================================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var investment = _context.Investments.FirstOrDefault(i => i.InvestmentId == id && i.UserId == userId);

            if (investment != null)
            {
                _context.Investments.Remove(investment);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Investment removed from portfolio.";
            }
            return RedirectToAction("Index");
        }
    }
}
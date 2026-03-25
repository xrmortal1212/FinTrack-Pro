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
    public class DebtController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DebtController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. INDEX (Udhaar ki List aur Stats)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var debts = await _context.Debts
                .Where(d => d.UserId == userId)
                .OrderBy(d => d.DueDate)
                .ToListAsync();

            // Safe calculation (agar list khali ho toh 0 return karega)
            ViewBag.ToPay = debts.Where(d => d.Type == "Borrowed").Sum(d => d.TotalAmount - d.PaidAmount);
            ViewBag.ToReceive = debts.Where(d => d.Type == "Lent").Sum(d => d.TotalAmount - d.PaidAmount);

            return View(debts);
        }

        // ==========================================
        // 2. CREATE (Naya Udhaar Add Karna)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Debt { DueDate = DateTime.Now.AddMonths(1) });
        }

        [HttpPost]
        public IActionResult Create(Debt debt)
        {
            debt.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                _context.Debts.Add(debt);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(debt);
        }

        // ==========================================
        // 3. PAY DEBT (Udhaar wapas karna / Qist dena)
        // ==========================================
        [HttpPost]
        public IActionResult PayDebt(int DebtId, decimal AmountToPay)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var debt = _context.Debts.FirstOrDefault(d => d.DebtId == DebtId && d.UserId == userId);

            if (debt != null && AmountToPay > 0)
            {
                debt.PaidAmount += AmountToPay;

                if (debt.PaidAmount > debt.TotalAmount)
                {
                    debt.PaidAmount = debt.TotalAmount;
                }

                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. DELETE DEBT (NEW)
        // ==========================================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var debt = _context.Debts.FirstOrDefault(d => d.DebtId == id && d.UserId == userId);

            if (debt != null)
            {
                _context.Debts.Remove(debt);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Debt record deleted successfully!";
            }
            return RedirectToAction("Index");
        }
    }
}

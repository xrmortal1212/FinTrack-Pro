using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FinTrack_Pro.Data;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Text.Json;

namespace FinTrack_Pro.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string filter = "ThisMonth")
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            DateTime startDate, endDate;
            var today = DateTime.Today;

            // 1. DATE FILTERS LOGIC 📅
            switch (filter)
            {
                case "LastMonth":
                    startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    endDate = new DateTime(today.Year, today.Month, 1).AddDays(-1);
                    break;
                case "ThisYear":
                    startDate = new DateTime(today.Year, 1, 1);
                    endDate = new DateTime(today.Year, 12, 31);
                    break;
                case "ThisMonth":
                default:
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                    break;
            }

            ViewBag.CurrentFilter = filter;
            ViewBag.StartDate = startDate.ToString("dd MMM yyyy");
            ViewBag.EndDate = endDate.ToString("dd MMM yyyy");

            // Fetch Transactions for the selected period
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Date.Date >= startDate && t.Date.Date <= endDate)
                .ToListAsync();

            // 2. SUMMARY CARDS 💰
            var totalIncome = transactions.Where(t => t.Category.Type == "Income").Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.Category.Type == "Expense").Sum(t => t.Amount);
            var netSavings = totalIncome - totalExpense;

            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.NetSavings = netSavings;

            // 3. CHART DATA: Expense Breakdown (Donut Chart) 🍩
            var expenseByCategory = transactions
                .Where(t => t.Category.Type == "Expense")
                .GroupBy(t => t.Category.Title)
                .Select(g => new { CategoryName = g.Key, TotalAmount = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            ViewBag.ExpenseLabels = JsonSerializer.Serialize(expenseByCategory.Select(e => e.CategoryName));
            ViewBag.ExpenseData = JsonSerializer.Serialize(expenseByCategory.Select(e => e.TotalAmount));

            // 4. TOP SPENDING CATEGORIES 🏆
            ViewBag.TopExpenses = expenseByCategory.Take(3).ToList();

            // =====================================
            // OLD CODE: ANNUAL TAX ESTIMATION (Always for Current Year)
            // =====================================
            var currentYear = DateTime.Now.Year;
            var yearlyIncome = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Category.Type == "Income" && t.Date.Year == currentYear)
                .SumAsync(t => t.Amount);

            decimal estimatedTax = yearlyIncome > 600000 ? (yearlyIncome - 600000) * 0.05m : 0;

            ViewBag.YearlyIncome = yearlyIncome;
            ViewBag.EstimatedTax = estimatedTax;
            ViewBag.CurrentYear = currentYear;

            return View();
        }
    }
}
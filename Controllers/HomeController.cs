using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FinTrack_Pro.Data;
using FinTrack_Pro.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace FinTrack_Pro.Controllers
{    
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
       
        // ==========================================
        // 1. PUBLIC WEBSITE (Landing Page)
        // ==========================================
        [AllowAnonymous] // Koi bhi (login ya bina login) is page par aa sakta hai
        public IActionResult Index()
        {
            // Ab yahan koi redirect nahi hai. Logged-in user bhi landing page dekh sakta hai.
            return View();
        }

        // ==========================================
        // 2. SECURE DASHBOARD (Login Ke Baad)
        // ==========================================
        [Authorize] // Yeh tag ensure karega ke yahan sirf logged-in users hi aayen
        public async Task<IActionResult> Dashboard()
        {
            // Logged-in user ki ID nikalna
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Database se user ka sabse pehla incomplete goal nikal rahe hain
            var activeGoal = _context.Goals
                .Where(g => g.UserId == userId && g.SavedAmount < g.TargetAmount)
                .OrderBy(g => g.Deadline)
                .FirstOrDefault();

            if (activeGoal != null)
            {
                ViewBag.GoalTitle = activeGoal.Title; // (Ya jo bhi naam ho apki property ka, e.g., Name/Title)
                ViewBag.GoalCurrentAmount = activeGoal.SavedAmount;
                ViewBag.GoalTargetAmount = activeGoal.TargetAmount;
            }
            else
            {
                // Agar user ka koi goal nahi hai toh default values
                ViewBag.GoalTitle = "No Active Goal";
                ViewBag.GoalCurrentAmount = 0;
                ViewBag.GoalTargetAmount = 1; // 1 rakha taake zero se divide error na aaye
            }

            // Aapka poora Dashboard ka logic (Income, Expense, Chart)
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId)
                .ToListAsync();

            var totalIncome = transactions.Where(t => t.Category != null && t.Category.Type == "Income").Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.Category != null && t.Category.Type == "Expense").Sum(t => t.Amount);
            

            // 3. Current Balance nikal lein
            var currentBalance = totalIncome - totalExpense;

            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.Balance = currentBalance.ToString("0.00");

            // Chart Logic
            var expenseData = transactions
                .Where(t => t.Category != null && t.Category.Type == "Expense")
                .GroupBy(t => t.Category.Title)
                .Select(g => new { CategoryName = g.Key, TotalAmount = g.Sum(t => t.Amount) })
                .ToList();

            ViewBag.ChartLabels = expenseData.Select(e => e.CategoryName).ToArray();
            ViewBag.ChartValues = expenseData.Select(e => e.TotalAmount).ToArray();

            // --- NAYA: Income Chart Logic ---
            var incomeData = transactions
                .Where(t => t.Category != null && t.Category.Type == "Income")
                .GroupBy(t => t.Category.Title)
                .Select(g => new { CategoryName = g.Key, TotalAmount = g.Sum(t => t.Amount) })
                .ToList();

            ViewBag.IncomeLabels = incomeData.Select(i => i.CategoryName).ToArray();
            ViewBag.IncomeValues = incomeData.Select(i => i.TotalAmount).ToArray();
            // --------------------------------

            // --- NAYA: Budget Trend Logic (Pichle 6 mahine ka) ---
            var trendLabels = new List<string>();
            var trendBudgets = new List<decimal>();
            var trendSpent = new List<decimal>();

            // Pichle 6 mahine ka loop chalayenge (e.g., Sep, Oct, Nov, Dec, Jan, Feb)
            for (int i = 5; i >= 0; i--)
            {
                var targetDate = DateTime.Now.AddMonths(-i);
                var targetMonth = targetDate.Month;
                var targetYear = targetDate.Year;

                // Mahine ka naam (e.g., "Jan")
                trendLabels.Add(targetDate.ToString("MMM"));

                // Us mahine ka Total Budget
                var monthlyBudget = _context.Budgets
                    .Where(b => b.UserId == userId && b.Month == targetMonth && b.Year == targetYear)
                    .Sum(b => b.Amount);

                // Us mahine ka Total Expense
                var monthlySpent = _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.UserId == userId && t.Category.Type == "Expense" && t.Date.Month == targetMonth && t.Date.Year == targetYear)
                    .Sum(t => t.Amount);

                trendBudgets.Add(monthlyBudget);
                trendSpent.Add(monthlySpent);
            }

            ViewBag.BudgetTrendLabels = trendLabels.ToArray();
            ViewBag.BudgetTrendLimits = trendBudgets.ToArray();
            ViewBag.BudgetTrendSpent = trendSpent.ToArray();
            // ---------------------------------------------------

            ViewBag.RecentUsers = _context.Users.Where(u => u.Id != userId).Take(4).ToList();

            // Sirf wo bills lao jo paid nahi hain (IsPaid == false) aur date ke hisaab se sort karo
            ViewBag.UpcomingBills = await _context.Bills
                .Where(b => b.UserId == userId && b.IsPaid == false)
                .OrderBy(b => b.DueDate)
                .Take(4) // Dashboard par sirf top 4 bills dikhayenge
                .ToListAsync();

            // -----------------------------------------------------

            // =========================================================
            // ACTIVE SUBSCRIPTIONS LOGIC
            // =========================================================
            var recurringBills = await _context.Bills
                .Where(b => b.UserId == userId && b.IsRecurring == true)
                .ToListAsync();

            var uniqueSubscriptions = recurringBills
                .GroupBy(b => b.Title)
                .Select(g => g.OrderByDescending(b => b.DueDate).First())
                .ToList();

            ViewBag.Subscriptions = uniqueSubscriptions.Select(s => s.Title).ToList();

            decimal totalMonthlyAmount = 0;

            foreach (var sub in uniqueSubscriptions)
            {
                switch (sub.RecurringFrequency)
                {
                    case "Daily":
                        totalMonthlyAmount += sub.Amount * 30;
                        break;
                    case "Weekly":
                        totalMonthlyAmount += sub.Amount * 4.33m; // 1 mahine mein taqreeban 4.33 haftay hote hain
                        break;
                    case "Monthly":
                        totalMonthlyAmount += sub.Amount; // Yeh toh pehle hi monthly hai
                        break;
                    case "Yearly":
                        totalMonthlyAmount += sub.Amount / 12m; // Saal ke amount ko 12 se divide kar diya
                        break;
                }
            }

            // Total ko string mein convert kar ke ViewBag mein bhej dein (2 decimal places ke sath)
            ViewBag.TotalSubscriptionAmount = totalMonthlyAmount.ToString("0.00");
       
            var recentTransactions = transactions.OrderByDescending(t => t.Date).Take(5).ToList();

            return View(recentTransactions);
        }



        public IActionResult Privacy()
        {
            return View();
        }

        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}

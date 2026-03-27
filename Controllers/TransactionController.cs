using FinTrack_Pro.Data;
using FinTrack_Pro.Models;
using FinTrack_Pro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinTrack_Pro.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notiService;

        public TransactionController(ApplicationDbContext context, NotificationService notiService)
        {
            _context = context;
            _notiService = notiService; // Inject kar li
        }

        // ==========================================
        // 1. INDEX (Transactions ki List)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // .Include(t => t.Category) is liye lagaya taake Transactions ke sath 
            // uski juri hui Category ka Title aur Type bhi nikal aaye!
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date) // Nayi transaction sab se upar aaye
                .ToListAsync();

            // Sirf current user ki categories layega aur "CategoryId" use karega
            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "CategoryId", "Title");

            // Income, Expense aur Balance calculate kar rahe hain
            decimal totalIncome = transactions.Where(t => t.Category?.Type == "Income").Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => t.Category?.Type == "Expense").Sum(t => t.Amount);
            decimal netBalance = totalIncome - totalExpense;

            // ViewBags ke zariye HTML ko bhej rahe hain
            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.NetBalance = netBalance;

            return View(transactions);
        }

        // ==========================================
        // 2. CREATE (Nayi Transaction ka Form)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "CategoryId", "Title");

            return View(new Transaction { Date = System.DateTime.Now });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Transaction transaction)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            transaction.UserId = userId;

            ModelState.Remove("User");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                _context.Transactions.Add(transaction);
                _context.SaveChanges();

                // =========================================================
                // SMART NOTIFICATION (Check if Income or Expense)
                // =========================================================
                var category = _context.Categories.FirstOrDefault(c => c.CategoryId == transaction.CategoryId);

                if (category != null && category.Type == "Income")
                {
                    // Agar Income hai toh Green Notification
                    await _notiService.SendNotificationAsync(
                        userId: userId,
                        title: "Income Added 💰",
                        message: $"Rs. {transaction.Amount} added to your wallet for {category.Title}.",
                        icon: "bi-wallet2",
                        color: "#C7EF4E", // Green
                        url: "/Transaction/Index"
                    );
                }
                else if (category != null && category.Type == "Expense")
                {
                    // Agar Expense hai toh Orange/Red Notification
                    await _notiService.SendNotificationAsync(
                        userId: userId,
                        title: "Expense Logged 📉",
                        message: $"You spent Rs. {transaction.Amount} on {category.Title}.",
                        icon: "bi-receipt-cutoff",
                        color: "#f15e36", // Orange
                        url: "/Transaction/Index"
                    );
                }

                return RedirectToAction("Index");
            }

           
            ViewBag.Categories = new SelectList(_context.Categories.Where(c => c.UserId == userId), "CategoryId", "Title", transaction.CategoryId);
            return View(transaction);
        }

        [HttpPost]
        public async Task<IActionResult> QuickTransfer(string ReceiverEmail, decimal Amount)
        {
            var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var sender = await _context.Users.FindAsync(senderId);

            // Ab hum unique Email se user dhoond rahe hain
            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Email == ReceiverEmail);

            if (receiver == null)
            {
                TempData["TransferError"] = "User nahi mila! Email check karein.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (senderId == receiver.Id)
            {
                TempData["TransferError"] = "Aap khud ko paise nahi bhej sakte!";
                return RedirectToAction("Dashboard", "Home");
            }

            // =========================================================
            // 1. SENDER KI CATEGORY CHECK KAREIN (EXPENSE)
            // =========================================================
            var senderCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.UserId == senderId && c.Type == "Expense" && c.Title == "Quick Transfer");

            // Agar category nahi mili, toh nayi bana do!
            if (senderCategory == null)
            {
                senderCategory = new Category
                {
                    UserId = senderId,
                    Title = "Quick Transfer",
                    Type = "Expense"
                    // Agar aapke Category model mein koi Icon wagerah ki property hai toh yahan de sakte hain
                };
                _context.Categories.Add(senderCategory);
                await _context.SaveChangesAsync(); // Yahan save kar rahe hain taake CategoryId mil jaye
            }

            // =========================================================
            // 2. RECEIVER KI CATEGORY CHECK KAREIN (INCOME)
            // =========================================================
            var receiverCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.UserId == receiver.Id && c.Type == "Income" && c.Title == "Quick Transfer");

            // Agar receiver ke paas bhi category nahi hai, toh bana do!
            if (receiverCategory == null)
            {
                receiverCategory = new Category
                {
                    UserId = receiver.Id,
                    Title = "Quick Transfer",
                    Type = "Income"
                };
                _context.Categories.Add(receiverCategory);
                await _context.SaveChangesAsync(); // Save to get the new CategoryId
            }

            // =========================================================
            // 3. TRANSACTIONS SAVE KAREIN (Yahan dhyan dein)
            // =========================================================

            // (A) Sender ke liye Expense Transaction
            var expenseTransaction = new Transaction
            {
                UserId = senderId,
                Amount = Amount,
                Date = DateTime.Now,
                // YAHAN 1 nahi likhna, dynamic ID likhni hai:
                CategoryId = senderCategory.CategoryId
            };

            // (B) Receiver ke liye Income Transaction
            var incomeTransaction = new Transaction
            {
                UserId = receiver.Id,
                Amount = Amount,
                Date = DateTime.Now,
                // YAHAN 2 nahi likhna, dynamic ID likhni hai:
                CategoryId = receiverCategory.CategoryId
            };

            // Purani wali Transaction add karne ki lines agar koi hain toh unhe delete kar dein
            _context.Transactions.Add(expenseTransaction);
            _context.Transactions.Add(incomeTransaction);
            await _context.SaveChangesAsync();

            // =========================================================
            // 4. DONO USERS KO NOTIFICATIONS BHEJEIN
            // =========================================================

            // (A) Sender ko alert (Kyunke iske paise gaye hain - Orange/Red color)
            await _notiService.SendNotificationAsync(
                userId: senderId,
                title: "Money Sent 💸",
                message: $"You have successfully sent Rs. {Amount} to {receiver.FullName}.",
                icon: "bi-send-check-fill",
                color: "#f15e36", // Aapka theme color (Expense)
                url: "/Transaction/Index"
            );

            // (B) Receiver ko alert (Kyunke iske paas paise aaye hain - Green color)
            await _notiService.SendNotificationAsync(
                userId: receiver.Id,
                title: "Money Received! 🎉",
                message: $"{sender.FullName} just sent you Rs. {Amount}.",
                icon: "bi-arrow-down-left-circle-fill",
                color: "#2ecc71", // Green color (Income)
                url: "/Transaction/Index"
            );

            TempData["TransferMessage"] = $"${Amount} successfully sent to {receiver.FullName}!";
            return RedirectToAction("Dashboard", "Home");
        }
    }
}

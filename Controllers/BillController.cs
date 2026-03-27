using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FinTrack_Pro.Data;
using FinTrack_Pro.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace FinTrack_Pro.Controllers
{
    [Authorize]
    public class BillController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Bill/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var bills = await _context.Bills
                .Where(b => b.UserId == userId)
                .OrderBy(b => b.IsPaid)
                .ThenBy(b => b.DueDate)
                .ToListAsync();

            // ==========================================
            // SUMMARY STATS CALCULATION
            // ==========================================
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            // 1. Total Unpaid Bills Amount
            ViewBag.TotalUnpaidAmount = bills.Where(b => !b.IsPaid).Sum(b => b.Amount);

            // 2. Total Paid Bills This Month
            ViewBag.PaidBillsCount = bills.Count(b => b.IsPaid && b.DueDate >= thisMonth);

            // 3. Next Due Bill
            var nextBill = bills.Where(b => !b.IsPaid && b.DueDate >= today)
                                .OrderBy(b => b.DueDate)
                                .FirstOrDefault();

            if (nextBill != null)
            {
                ViewBag.NextBillTitle = nextBill.Title;
                ViewBag.NextBillDays = (nextBill.DueDate - today).Days; // Din calculate karo
            }
            else
            {
                ViewBag.NextBillTitle = "None";
                ViewBag.NextBillDays = -1;
            }

            return View(bills);
        }

        // GET: Bill/Add
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }
        // POST: Bill/Add
        [HttpPost]
        public async Task<IActionResult> Add(Bill model)
        {
            // 🚨 YEH LINES ADD KAREIN: 
            // Validation check se pehle in fields ko ignore karwayen kyunke hum inhe khud set kar rahe hain.
            ModelState.Remove("UserId");
            ModelState.Remove("IsPaid");
            ModelState.Remove("IsRecurring");
            // Note: Agar aapke Bill model mein public virtual User User {get; set;} jaisi property hai, toh niche wali line bhi zaroor likhein:
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                model.UserId = userId;
                model.IsPaid = false;

                // Check karein recurring hai ya nahi
                if (model.RecurringFrequency != "None" && !string.IsNullOrEmpty(model.RecurringFrequency))
                {
                    model.IsRecurring = true;
                }
                else
                {
                    model.IsRecurring = false;
                    model.RecurringFrequency = "None"; // Safe side ke liye
                }

                _context.Bills.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{model.Title} bill added successfully!";

                // Aap chahain toh isko RedirectToAction("Index", "Bill") bhi kar sakte hain taake user ko add hone ke baad list nazar aaye
                return RedirectToAction("Dashboard", "Home");
            }

            // Agar koi form validation fail ho jaye toh wapas view dikhaye ga
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PayBill(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // 1. Bill dhoondo jo is user ka hai
            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (bill != null && !bill.IsPaid)
            {
                // =========================================================
                // 2. AUTO-CREATE CATEGORY LOGIC
                // =========================================================
                var billCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.Type == "Expense" && c.Title == "Bills");

                if (billCategory == null)
                {
                    billCategory = new Category
                    {
                        UserId = userId,
                        Title = "Bills",
                        Type = "Expense"
                    };
                    _context.Categories.Add(billCategory);
                    await _context.SaveChangesAsync(); // Nayi ID lene ke liye save karein
                }

                // =========================================================
                // 4. RECURRING BILL LOGIC: Naya bill auto-generate karein
                // =========================================================
                if (bill.IsRecurring && bill.RecurringFrequency != "None")
                {
                    DateTime nextDueDate = bill.DueDate;

                    // Date ko frequency ke hisaab se aage barhayen
                    switch (bill.RecurringFrequency)
                    {
                        case "Daily":
                            nextDueDate = nextDueDate.AddDays(1);
                            break;
                        case "Weekly":
                            nextDueDate = nextDueDate.AddDays(7);
                            break;
                        case "Monthly":
                            nextDueDate = nextDueDate.AddMonths(1);
                            break;
                        case "Yearly":
                            nextDueDate = nextDueDate.AddYears(1);
                            break;
                    }

                    // Ek naya unpaid bill database mein daal dein
                    var nextBill = new Bill
                    {
                        UserId = bill.UserId,
                        Title = bill.Title,
                        Amount = bill.Amount,
                        DueDate = nextDueDate,
                        IsPaid = false, // Yeh naya bill hai, isliye unpaid hoga
                        IsRecurring = true,
                        RecurringFrequency = bill.RecurringFrequency
                    };

                    _context.Bills.Add(nextBill);
                    await _context.SaveChangesAsync();
                }

                // =========================================================
                // 3. BILL KO PAID MARK KAREIN AUR TRANSACTION ADD KAREIN
                // =========================================================

                bill.IsPaid = true; // Dashboard se hatane ke liye

                var transaction = new Transaction
                {
                    UserId = userId,
                    Amount = bill.Amount,
                    Date = DateTime.Now,
                    Description = $"Paid Bill: {bill.Title}", // Transaction me detail aa jayegi
                    CategoryId = billCategory.CategoryId // Auto-created category ki ID
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{bill.Title} bill paid successfully!";
            }

            return RedirectToAction("Dashboard", "Home");
        }

        // POST: Bill/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Database mein bill dhoondo jo is user ka ho
            var bill = await _context.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (bill != null)
            {
                _context.Bills.Remove(bill);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{bill.Title} bill deleted successfully!";
            }

            return RedirectToAction("Index");
        }
    }
}
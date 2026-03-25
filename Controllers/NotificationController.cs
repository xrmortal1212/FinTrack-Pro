using FinTrack_Pro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinTrack_Pro.Controllers
{
    [Authorize] // Sirf logged-in users ke liye
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. INDEX: Saari Notifications Dikhane Ke Liye
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // User ID nikalna
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // User ki saari notifications Database se lana (Nayi sabse upar)
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // ==========================================
        // 2. MARK ALL AS READ: Red dots khatam karne ke liye
        // ==========================================
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Sirf wo notifications uthao jo abhi tak read nahi hui
            var unreadNotis = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            // Sab ko 'Read' (true) kar do
            foreach (var noti in unreadNotis)
            {
                noti.IsRead = true;
            }

            await _context.SaveChangesAsync();

            // User ko wapis usi page par bhej do jahan se usne click kiya tha
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }

            return RedirectToAction("Dashboard", "Home");
        }
    }
}
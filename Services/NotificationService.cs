using FinTrack_Pro.Data;
using FinTrack_Pro.Models;
using System.Threading.Tasks;

namespace FinTrack_Pro.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Yeh function hum kisi bhi Controller se call karenge
        public async Task SendNotificationAsync(int userId, string title, string message, string icon, string color, string url)
        {
            var noti = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Icon = icon,
                Color = color,
                ActionUrl = url
            };

            _context.Notifications.Add(noti);
            await _context.SaveChangesAsync();
        }
    }
}
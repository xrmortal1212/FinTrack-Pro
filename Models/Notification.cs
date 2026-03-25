using System;

namespace FinTrack_Pro.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Kis user ki notification hai
        public string Title { get; set; } // e.g., "Money Transferred"
        public string Message { get; set; } // e.g., "You sent Rs.500 to Sidhan."
        public string Icon { get; set; } // FontAwesome ya Bootstrap icon ki class, e.g., "bi-send"
        public string Color { get; set; } // Hex code color ke liye e.g., "#3498db"
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Kab aayi?
        public bool IsRead { get; set; } = false; // User ne parh li ya nahi?
        public string ActionUrl { get; set; } // Click karne pe kis page pe jaye?

        // Agar aapne User model banaya hai toh Relation ke liye:
        public User User { get; set; }
    }
}
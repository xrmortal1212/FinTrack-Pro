using System;
using System.ComponentModel.DataAnnotations;

namespace FinTrack_Pro.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please Enter Your Full Name. ")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required. ")]
        [EmailAddress(ErrorMessage = "Invalid Email Address. ")]
        public string Email { get; set; }

        // [Required(ErrorMessage = "Password is required. ")] <-- Isey hata dein ya comment kar dein
        public string? PasswordHash { get; set; } // Isey nullable (?) kar dein

        [StringLength(20)]
        public string Role { get; set; } = "User"; // Default role "User" hoga

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ==========================================
        // Nayi Profile Fields (Optional / Nullable)
        // ==========================================

        [StringLength(255)]
        public string? ProfilePicturePath { get; set; } // Pfp store karne ke liye

        [StringLength(255)]
        public string? BannerPath { get; set; } // Cover/Banner image store karne ke liye

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string? Bio { get; set; } // User ke baare mein choti si description

        [Phone(ErrorMessage = "Invalid Phone Number.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; } // Contact number

        [StringLength(100)]
        public string? Profession { get; set; } // e.g., Freelancer, Banker, Student
    }
}

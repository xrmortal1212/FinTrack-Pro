using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinTrack_Pro.Models
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Bill Title is required.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public bool IsRecurring { get; set; }

        public string RecurringFrequency { get; set; } // None, Daily, Weekly, Monthly, Yearly

        public bool IsPaid { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
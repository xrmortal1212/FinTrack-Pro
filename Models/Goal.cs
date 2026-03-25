using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinTrack_Pro.Models
{
    public class Goal
    {
        [Key]
        public int GoalId { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required(ErrorMessage = "Please enter a goal title.")]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter tje targer amount.")]
        [Range(1, double.MaxValue, ErrorMessage = "Target must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SavedAmount { get; set; } = 0;

        [Required(ErrorMessage = "Please select a deadline.")]
        public DateTime Deadline { get; set; }
    }
}

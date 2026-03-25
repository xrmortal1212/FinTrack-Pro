using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinTrack_Pro.Models
{
    public class Debt
    {
        [Key]
        public int DebtId { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required(ErrorMessage = "Source name is required.")]
        [StringLength(100)]
        public string SourceName { get; set; }

        // NAYA COLUMN: Borrowed (Udhaar Liya) ya Lent (Udhaar Diya)
        [Required]
        [StringLength(20)]
        public string Type { get; set; }

        // NAYA COLUMN: Udhaar kis liye tha? (Optional)
        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Required]
        public DateTime DueDate { get; set; }
    }
}
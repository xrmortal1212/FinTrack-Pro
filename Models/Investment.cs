using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinTrack_Pro.Models
{
    public class Investment
    {
        [Key]
        public int InvestmentId { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required(ErrorMessage = "Asset name is required.")]
        [StringLength(100)]
        public string AssetName { get; set; }

        // 🌟 NAYA COLUMN: Category (e.g., Crypto, Stocks, Gold, Real Estate)
        [Required(ErrorMessage = "Please select a category.")]
        [StringLength(50)]
        public string Category { get; set; }

        // 🌟 NAYA COLUMN (Optional): Future API ke liye Ticker symbol (e.g., BTC, AAPL)
        [StringLength(20)]
        public string Symbol { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InvestedAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; }
    }
}
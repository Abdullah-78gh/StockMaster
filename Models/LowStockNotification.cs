using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Models
{
    public class LowStockNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int CurrentQuantity { get; set; }

        public int MinimumStockLevel { get; set; }

        public bool IsResolved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedBy { get; set; }

        [StringLength(500)]
        public string? ResolutionNotes { get; set; }
    }
}
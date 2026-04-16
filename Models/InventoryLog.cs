using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Models
{
    public enum InventoryAction
    {
        StockIn,
        StockOut,
        Adjustment,
        Sale,
        Purchase,
        Return,
        Damaged,
        Lost
    }

    public class InventoryLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        public InventoryAction Action { get; set; }

        [Required]
        public int Quantity { get; set; }

        public int PreviousQuantity { get; set; }

        public int NewQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public int PerformedBy { get; set; }

        [ForeignKey("PerformedBy")]
        public virtual User? User { get; set; }

        public string? ReferenceNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
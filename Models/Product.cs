using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockMaster.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Barcode { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        [Required]
        public int QuantityInStock { get; set; }

        public int MinimumStockLevel { get; set; } = 5;

        public int MaximumStockLevel { get; set; } = 100;

        public int ReorderPoint { get; set; } = 10;

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string? UnitOfMeasure { get; set; } = "Piece";

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Weight { get; set; }

        [StringLength(50)]
        public string? WeightUnit { get; set; }

        public string? Location { get; set; }
    }
}
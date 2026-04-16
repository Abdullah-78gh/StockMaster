using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockMaster.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product Name is required")]
        [StringLength(100, ErrorMessage = "Product Name cannot exceed 100 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "SKU is required")]
        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        [Display(Name = "SKU")]
        public string SKU { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Barcode cannot exceed 100 characters")]
        [Display(Name = "Barcode")]
        public string? Barcode { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Cost is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Cost")]
        public decimal Cost { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
        [Display(Name = "Quantity in Stock")]
        public int QuantityInStock { get; set; }

        [Required(ErrorMessage = "Minimum Stock Level is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Minimum stock level must be at least 1")]
        [Display(Name = "Minimum Stock Level")]
        public int MinimumStockLevel { get; set; }

        [Display(Name = "Maximum Stock Level")]
        public int MaximumStockLevel { get; set; } = 100;

        [Display(Name = "Reorder Point")]
        public int ReorderPoint { get; set; } = 10;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Unit of Measure")]
        public string? UnitOfMeasure { get; set; } = "Piece";

        [Display(Name = "Weight")]
        public decimal? Weight { get; set; }

        [Display(Name = "Weight Unit")]
        public string? WeightUnit { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // For dropdown list
        public SelectList? CategoryList { get; set; }
    }
}
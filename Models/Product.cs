using System.ComponentModel.DataAnnotations;

namespace ShoppingBasketAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        
        public bool IsDiscounted { get; set; } = false;
        
        public decimal? DiscountPercentage { get; set; }
        
        public string? Description { get; set; }
        
        public string? Category { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
} 
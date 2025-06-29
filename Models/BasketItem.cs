using System.ComponentModel.DataAnnotations;

namespace ShoppingBasketAPI.Models
{
    public class BasketItem
    {
        public int Id { get; set; }
        
        public int BasketId { get; set; }
        
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Basket Basket { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
} 
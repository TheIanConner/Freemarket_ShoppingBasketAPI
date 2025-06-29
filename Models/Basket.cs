using System.ComponentModel.DataAnnotations;

namespace ShoppingBasketAPI.Models
{
    public class Basket
    {
        public int Id { get; set; }
        
        [Required]
        public string SessionId { get; set; } = string.Empty;
        
        public string? DiscountCode { get; set; }
        
        public decimal? DiscountPercentage { get; set; }
        
        public string? ShippingCountry { get; set; }
        
        public decimal ShippingCost { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<BasketItem> Items { get; set; } = new List<BasketItem>();
    }
} 
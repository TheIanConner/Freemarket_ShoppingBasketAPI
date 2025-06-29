using System.ComponentModel.DataAnnotations;

namespace ShoppingBasketAPI.DTOs
{
    public class RemoveItemRequest
    {
        [Required]
        public int ProductId { get; set; }
        
        [Range(1, int.MaxValue)]
        public int? Quantity { get; set; } // If null, remove all of this item
    }
} 
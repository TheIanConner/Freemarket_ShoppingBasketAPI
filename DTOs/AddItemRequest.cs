using System.ComponentModel.DataAnnotations;

namespace ShoppingBasketAPI.DTOs
{
    public class AddItemRequest
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
} 
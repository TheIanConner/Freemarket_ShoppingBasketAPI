using System.ComponentModel.DataAnnotations;

namespace ShoppingBasketAPI.DTOs
{
    public class AddMultipleItemsRequest
    {
        [Required]
        [MinLength(1)]
        public List<AddItemRequest> Items { get; set; } = new List<AddItemRequest>();
    }
} 
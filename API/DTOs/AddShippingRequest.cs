using System.ComponentModel.DataAnnotations;

namespace ShoppingBasketAPI.DTOs
{
    public class AddShippingRequest
    {
        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;
    }
} 
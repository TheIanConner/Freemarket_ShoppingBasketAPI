using System.ComponentModel.DataAnnotations;

namespace ShoppingBasketAPI.DTOs
{
    public class AddDiscountCodeRequest
    {
        [Required]
        [MaxLength(50)]
        public string DiscountCode { get; set; } = string.Empty;
    }
} 
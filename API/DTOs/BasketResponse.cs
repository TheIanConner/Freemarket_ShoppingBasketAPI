namespace ShoppingBasketAPI.DTOs
{
    public class BasketResponse
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public List<BasketItemResponse> Items { get; set; } = new List<BasketItemResponse>();
        public string? DiscountCode { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public string? ShippingCountry { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalWithoutVat { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalWithVat { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class BasketItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsDiscounted { get; set; }
        public decimal? ProductDiscountPercentage { get; set; }
    }
} 
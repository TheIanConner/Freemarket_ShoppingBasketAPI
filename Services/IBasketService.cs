using ShoppingBasketAPI.DTOs;

namespace ShoppingBasketAPI.Services
{
    public interface IBasketService
    {
        Task<BasketResponse> GetBasketAsync(string sessionId);
        Task<BasketResponse> AddItemAsync(string sessionId, AddItemRequest request);
        Task<BasketResponse> AddMultipleItemsAsync(string sessionId, AddMultipleItemsRequest request);
        Task<BasketResponse> RemoveItemAsync(string sessionId, RemoveItemRequest request);
        Task<BasketResponse> AddDiscountCodeAsync(string sessionId, AddDiscountCodeRequest request);
        Task<BasketResponse> ClearDiscountCodeAsync(string sessionId);
        Task<BasketResponse> AddShippingAsync(string sessionId, AddShippingRequest request);
        Task<BasketResponse> GetTotalCostAsync(string sessionId);
        Task<BasketResponse> GetTotalCostWithoutVatAsync(string sessionId);
        Task<BasketResponse> ClearBasketAsync(string sessionId);
    }
} 
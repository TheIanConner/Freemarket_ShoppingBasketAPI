using Microsoft.EntityFrameworkCore;
using ShoppingBasketAPI.Data;
using ShoppingBasketAPI.DTOs;
using ShoppingBasketAPI.Models;

namespace ShoppingBasketAPI.Services
{
    public class BasketService(ShoppingBasketContext context) : IBasketService
    {
        private readonly ShoppingBasketContext _context = context;
        private const decimal VAT_RATE = 0.20m; // 20% VAT
        private const decimal UK_SHIPPING_COST = 5.99m;
        private const decimal INTERNATIONAL_SHIPPING_COST = 15.99m;

        public async Task<BasketResponse> GetBasketAsync(string sessionId)
        {
            var basket = await this.GetOrCreateBasketAsync(sessionId);
            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> AddItemAsync(string sessionId, AddItemRequest request)
        {
            var basket = await this.GetOrCreateBasketAsync(sessionId);
            var product = await this.GetProductAsync(request.ProductId);

            var existingItem = await this._context.BasketItems
                .FirstOrDefaultAsync(bi => bi.BasketId == basket.Id && bi.ProductId == request.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var basketItem = new BasketItem
                {
                    BasketId = basket.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    UnitPrice = product.Price
                };
                this._context.BasketItems.Add(basketItem);
            }

            basket.UpdatedAt = DateTime.UtcNow;
            await this._context.SaveChangesAsync();

            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> AddMultipleItemsAsync(string sessionId, AddMultipleItemsRequest request)
        {
            var basket = await this.GetOrCreateBasketAsync(sessionId);

            foreach (var itemRequest in request.Items)
            {
                var product = await this.GetProductAsync(itemRequest.ProductId);
                var existingItem = await this._context.BasketItems
                    .FirstOrDefaultAsync(bi => bi.BasketId == basket.Id && bi.ProductId == itemRequest.ProductId);

                if (existingItem != null)
                {
                    existingItem.Quantity += itemRequest.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var basketItem = new BasketItem
                    {
                        BasketId = basket.Id,
                        ProductId = itemRequest.ProductId,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = product.Price
                    };
                    this._context.BasketItems.Add(basketItem);
                }
            }

            basket.UpdatedAt = DateTime.UtcNow;
            await this._context.SaveChangesAsync();

            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> RemoveItemAsync(string sessionId, RemoveItemRequest request)
        {
            var basket = await this._context.Baskets
                .Include(b => b.Items)
                .ThenInclude(bi => bi.Product)
                .FirstOrDefaultAsync(b => b.SessionId == sessionId) ?? throw new InvalidOperationException("Basket not found");
            var existingItem = await this._context.BasketItems
                .FirstOrDefaultAsync(bi => bi.BasketId == basket.Id && bi.ProductId == request.ProductId) ?? throw new InvalidOperationException("Item not found in basket");
            if (request.Quantity.HasValue)
            {
                if (request.Quantity.Value >= existingItem.Quantity)
                {
                    this._context.BasketItems.Remove(existingItem);
                }
                else
                {
                    existingItem.Quantity -= request.Quantity.Value;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                this._context.BasketItems.Remove(existingItem);
            }

            basket.UpdatedAt = DateTime.UtcNow;
            await this._context.SaveChangesAsync();

            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> AddDiscountCodeAsync(string sessionId, AddDiscountCodeRequest request)
        {
            var basket = await this.GetOrCreateBasketAsync(sessionId);

            // Simple discount code logic - in a real application, this would be more complex
            var discountPercentage = request.DiscountCode.ToUpper() switch
            {
                "SAVE10" => 10.0m,
                "SAVE20" => 20.0m,
                "SAVE25" => 25.0m,
                _ => throw new InvalidOperationException("Invalid discount code")
            };

            basket.DiscountCode = request.DiscountCode;
            basket.DiscountPercentage = discountPercentage;
            basket.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync();

            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> ClearDiscountCodeAsync(string sessionId)
        {
            var basket = await this._context.Baskets
                .Include(b => b.Items)
                .ThenInclude(bi => bi.Product)
                .FirstOrDefaultAsync(b => b.SessionId == sessionId) ?? throw new InvalidOperationException("Basket not found");
            basket.DiscountCode = null;
            basket.DiscountPercentage = null;
            basket.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync();

            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> AddShippingAsync(string sessionId, AddShippingRequest request)
        {
            var basket = await this.GetOrCreateBasketAsync(sessionId);

            basket.ShippingCountry = request.Country;
            basket.ShippingCost = request.Country.Equals("UK", StringComparison.CurrentCultureIgnoreCase) ? UK_SHIPPING_COST : INTERNATIONAL_SHIPPING_COST;
            basket.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync();

            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> GetTotalCostAsync(string sessionId)
        {
            var basket = await this.GetOrCreateBasketAsync(sessionId);
            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> GetTotalCostWithoutVatAsync(string sessionId)
        {
            var basket = await this.GetOrCreateBasketAsync(sessionId);
            return await this.MapToBasketResponseAsync(basket);
        }

        public async Task<BasketResponse> ClearBasketAsync(string sessionId)
        {
            var basket = await this._context.Baskets
                .Include(b => b.Items)
                .ThenInclude(bi => bi.Product)
                .FirstOrDefaultAsync(b => b.SessionId == sessionId) ?? throw new InvalidOperationException("Basket not found");

            // Remove all items first (cascade delete should handle this, but being explicit)
            var items = await this._context.BasketItems
                .Where(bi => bi.BasketId == basket.Id)
                .ToListAsync();

            this._context.BasketItems.RemoveRange(items);

            // Remove the basket itself
            this._context.Baskets.Remove(basket);

            await this._context.SaveChangesAsync();

            // Return an empty basket response since the basket no longer exists
            return new BasketResponse
            {
                Id = 0,
                SessionId = sessionId,
                Items = [],
                DiscountCode = null,
                DiscountPercentage = null,
                ShippingCountry = null,
                ShippingCost = 0,
                Subtotal = 0,
                DiscountAmount = 0,
                TotalWithoutVat = 0,
                VatAmount = 0,
                TotalWithVat = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task<Basket> GetOrCreateBasketAsync(string sessionId)
        {
            var basket = await this._context.Baskets
                .Include(b => b.Items)
                .ThenInclude(bi => bi.Product)
                .FirstOrDefaultAsync(b => b.SessionId == sessionId);

            if (basket == null)
            {
                basket = new Basket { SessionId = sessionId };
                this._context.Baskets.Add(basket);
                await this._context.SaveChangesAsync();
            }

            return basket;
        }

        private async Task<Product> GetProductAsync(int productId)
        {
            var product = await this._context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
                throw new InvalidOperationException($"Product with ID {productId} not found or inactive");

            return product;
        }

        private async Task<BasketResponse> MapToBasketResponseAsync(Basket basket)
        {
            var basketItems = await this._context.BasketItems
                .Include(bi => bi.Product)
                .Where(bi => bi.BasketId == basket.Id)
                .ToListAsync();

            var itemResponses = basketItems.Select(bi => new BasketItemResponse
            {
                Id = bi.Id,
                ProductId = bi.ProductId,
                ProductName = bi.Product.Name,
                Quantity = bi.Quantity,
                UnitPrice = bi.UnitPrice,
                TotalPrice = bi.Quantity * bi.UnitPrice,
                IsDiscounted = bi.Product.IsDiscounted,
                ProductDiscountPercentage = bi.Product.DiscountPercentage
            }).ToList();

            var subtotal = itemResponses.Sum(item => item.TotalPrice);
            var discountAmount = 0m;

            // Apply basket discount (excluding already discounted items)
            if (basket.DiscountPercentage.HasValue)
            {
                var nonDiscountedItemsTotal = itemResponses
                    .Where(item => !item.IsDiscounted)
                    .Sum(item => item.TotalPrice);
                discountAmount = nonDiscountedItemsTotal * (basket.DiscountPercentage.Value / 100);
            }

            var totalWithoutVat = subtotal - discountAmount + basket.ShippingCost;
            var vatAmount = totalWithoutVat * VAT_RATE;
            var totalWithVat = totalWithoutVat + vatAmount;

            return new BasketResponse
            {
                Id = basket.Id,
                SessionId = basket.SessionId,
                Items = itemResponses,
                DiscountCode = basket.DiscountCode,
                DiscountPercentage = basket.DiscountPercentage,
                ShippingCountry = basket.ShippingCountry,
                ShippingCost = basket.ShippingCost,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                TotalWithoutVat = totalWithoutVat,
                VatAmount = vatAmount,
                TotalWithVat = totalWithVat,
                CreatedAt = basket.CreatedAt,
                UpdatedAt = basket.UpdatedAt
            };
        }
    }
} 

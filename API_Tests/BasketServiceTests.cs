using Microsoft.EntityFrameworkCore;
using ShoppingBasketAPI.Data;
using ShoppingBasketAPI.DTOs;
using ShoppingBasketAPI.Models;
using ShoppingBasketAPI.Services;

namespace Tests
{
    public class BasketServiceTests : IDisposable
    {
        private readonly ShoppingBasketContext _context;
        private readonly BasketService _basketService;
        private const string TestSessionId = "test-session-123";
        
        public BasketServiceTests()
        {
            var options = new DbContextOptionsBuilder<ShoppingBasketContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new ShoppingBasketContext(options);
            _basketService = new BasketService(_context);
            
            SeedTestData();
        }
        
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
        
        private void SeedTestData()
        {
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Laptop", Price = 999.99m, IsActive = true },
                new Product { Id = 2, Name = "Mouse", Price = 25.50m, IsActive = true },
                new Product { Id = 3, Name = "Keyboard", Price = 75.00m, IsActive = true },
                new Product { Id = 4, Name = "Discounted Product", Price = 100.00m, IsActive = true, IsDiscounted = true, DiscountPercentage = 15.0m },
                new Product { Id = 5, Name = "Inactive Product", Price = 50.00m, IsActive = false }
            };
            
            _context.Products.AddRange(products);
            _context.SaveChanges();
        }
        
        #region GetBasketAsync Tests
        
        [Fact]
        public async Task GetBasketAsync_NewSession_ShouldCreateEmptyBasket()
        {
            // Act
            var result = await _basketService.GetBasketAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(TestSessionId, result.SessionId);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Subtotal);
            Assert.Equal(0, result.TotalWithVat);
            Assert.Null(result.DiscountCode);
            Assert.Null(result.ShippingCountry);
        }
        
        [Fact]
        public async Task GetBasketAsync_ExistingSession_ShouldReturnExistingBasket()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 2 });
            
            // Act
            var result = await _basketService.GetBasketAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(TestSessionId, result.SessionId);
            Assert.Single(result.Items);
            Assert.Equal(2, result.Items[0].Quantity);
        }
        
        #endregion
        
        #region AddItemAsync Tests
        
        [Fact]
        public async Task AddItemAsync_NewItem_ShouldAddItemToBasket()
        {
            // Arrange
            var request = new AddItemRequest { ProductId = 1, Quantity = 2 };
            
            // Act
            var result = await _basketService.AddItemAsync(TestSessionId, request);
            
            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.Items[0].ProductId);
            Assert.Equal("Laptop", result.Items[0].ProductName);
            Assert.Equal(2, result.Items[0].Quantity);
            Assert.Equal(999.99m, result.Items[0].UnitPrice);
            Assert.Equal(1999.98m, result.Items[0].TotalPrice);
        }
        
        [Fact]
        public async Task AddItemAsync_ExistingItem_ShouldIncreaseQuantity()
        {
            // Arrange
            var initialRequest = new AddItemRequest { ProductId = 1, Quantity = 2 };
            var additionalRequest = new AddItemRequest { ProductId = 1, Quantity = 3 };
            
            // Act
            await _basketService.AddItemAsync(TestSessionId, initialRequest);
            var result = await _basketService.AddItemAsync(TestSessionId, additionalRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(5, result.Items[0].Quantity); // 2 + 3
            Assert.Equal(4999.95m, result.Items[0].TotalPrice); // 5 * 999.99
        }
        
        [Fact]
        public async Task AddItemAsync_InvalidProduct_ShouldThrowException()
        {
            // Arrange
            var request = new AddItemRequest { ProductId = 999, Quantity = 1 };
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _basketService.AddItemAsync(TestSessionId, request)
            );
        }
        
        [Fact]
        public async Task AddItemAsync_InactiveProduct_ShouldThrowException()
        {
            // Arrange
            var request = new AddItemRequest { ProductId = 5, Quantity = 1 }; // Product 5 is inactive
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _basketService.AddItemAsync(TestSessionId, request)
            );
        }
        
        #endregion
        
        #region AddMultipleItemsAsync Tests
        
        [Fact]
        public async Task AddMultipleItemsAsync_MultipleNewItems_ShouldAddAllItems()
        {
            // Arrange
            var request = new AddMultipleItemsRequest
            {
                Items = new List<AddItemRequest>
                {
                    new AddItemRequest { ProductId = 1, Quantity = 2 },
                    new AddItemRequest { ProductId = 2, Quantity = 3 },
                    new AddItemRequest { ProductId = 3, Quantity = 1 }
                }
            };
            
            // Act
            var result = await _basketService.AddMultipleItemsAsync(TestSessionId, request);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Items.Count);
            
            var laptopItem = result.Items.First(i => i.ProductId == 1);
            Assert.Equal(2, laptopItem.Quantity);
            
            var mouseItem = result.Items.First(i => i.ProductId == 2);
            Assert.Equal(3, mouseItem.Quantity);
            
            var keyboardItem = result.Items.First(i => i.ProductId == 3);
            Assert.Equal(1, keyboardItem.Quantity);
        }
        
        [Fact]
        public async Task AddMultipleItemsAsync_MixOfNewAndExisting_ShouldUpdateQuantities()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 1 });
            
            var request = new AddMultipleItemsRequest
            {
                Items = new List<AddItemRequest>
                {
                    new AddItemRequest { ProductId = 1, Quantity = 2 }, // Should add to existing
                    new AddItemRequest { ProductId = 2, Quantity = 1 }  // New item
                }
            };
            
            // Act
            var result = await _basketService.AddMultipleItemsAsync(TestSessionId, request);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
            
            var laptopItem = result.Items.First(i => i.ProductId == 1);
            Assert.Equal(3, laptopItem.Quantity); // 1 + 2
            
            var mouseItem = result.Items.First(i => i.ProductId == 2);
            Assert.Equal(1, mouseItem.Quantity);
        }
        
        #endregion
        
        #region RemoveItemAsync Tests
        
        [Fact]
        public async Task RemoveItemAsync_PartialQuantity_ShouldReduceQuantity()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 5 });
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 2 };
            
            // Act
            var result = await _basketService.RemoveItemAsync(TestSessionId, removeRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(3, result.Items[0].Quantity); // 5 - 2
        }
        
        [Fact]
        public async Task RemoveItemAsync_FullQuantity_ShouldRemoveItem()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 3 });
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 3 };
            
            // Act
            var result = await _basketService.RemoveItemAsync(TestSessionId, removeRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }
        
        [Fact]
        public async Task RemoveItemAsync_QuantityExceedsAvailable_ShouldRemoveItem()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 2 });
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 5 };
            
            // Act
            var result = await _basketService.RemoveItemAsync(TestSessionId, removeRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }
        
        [Fact]
        public async Task RemoveItemAsync_NoQuantitySpecified_ShouldRemoveAllOfItem()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 5 });
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = null };
            
            // Act
            var result = await _basketService.RemoveItemAsync(TestSessionId, removeRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }
        
        [Fact]
        public async Task RemoveItemAsync_NonExistentItem_ShouldThrowException()
        {
            // Arrange
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 1 };
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _basketService.RemoveItemAsync(TestSessionId, removeRequest)
            );
        }
        
        [Fact]
        public async Task RemoveItemAsync_NonExistentBasket_ShouldThrowException()
        {
            // Arrange
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 1 };
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _basketService.RemoveItemAsync("non-existent-session", removeRequest)
            );
        }
        
        #endregion
        
        #region Discount Code Tests
        
        [Fact]
        public async Task AddDiscountCodeAsync_ValidCode_ShouldApplyDiscount()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 1 });
            var discountRequest = new AddDiscountCodeRequest { DiscountCode = "SAVE10" };
            
            // Act
            var result = await _basketService.AddDiscountCodeAsync(TestSessionId, discountRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("SAVE10", result.DiscountCode);
            Assert.Equal(10.0m, result.DiscountPercentage);
            Assert.Equal(99.999m, result.DiscountAmount); // 10% of 999.99
        }
        
        [Fact]
        public async Task AddDiscountCodeAsync_InvalidCode_ShouldThrowException()
        {
            // Arrange
            var discountRequest = new AddDiscountCodeRequest { DiscountCode = "INVALID" };
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _basketService.AddDiscountCodeAsync(TestSessionId, discountRequest)
            );
        }
        
        [Fact]
        public async Task AddDiscountCodeAsync_Save20Code_ShouldApply20PercentDiscount()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 2 }); // Mouse: 25.50 * 2 = 51.00
            var discountRequest = new AddDiscountCodeRequest { DiscountCode = "SAVE20" };
            
            // Act
            var result = await _basketService.AddDiscountCodeAsync(TestSessionId, discountRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("SAVE20", result.DiscountCode);
            Assert.Equal(20.0m, result.DiscountPercentage);
            Assert.Equal(10.20m, result.DiscountAmount); // 20% of 51.00
        }
        
        [Fact]
        public async Task ClearDiscountCodeAsync_WithDiscount_ShouldRemoveDiscount()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 1 });
            await _basketService.AddDiscountCodeAsync(TestSessionId, new AddDiscountCodeRequest { DiscountCode = "SAVE10" });
            
            // Act
            var result = await _basketService.ClearDiscountCodeAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Null(result.DiscountCode);
            Assert.Null(result.DiscountPercentage);
            Assert.Equal(0m, result.DiscountAmount);
        }
        
        [Fact]
        public async Task ClearDiscountCodeAsync_NonExistentBasket_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _basketService.ClearDiscountCodeAsync("non-existent-session")
            );
        }
        
        #endregion
        
        #region Shipping Tests
        
        [Fact]
        public async Task AddShippingAsync_UKShipping_ShouldApplyUKRate()
        {
            // Arrange
            var shippingRequest = new AddShippingRequest { Country = "UK" };
            
            // Act
            var result = await _basketService.AddShippingAsync(TestSessionId, shippingRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("UK", result.ShippingCountry);
            Assert.Equal(5.99m, result.ShippingCost);
        }
        
        [Fact]
        public async Task AddShippingAsync_InternationalShipping_ShouldApplyInternationalRate()
        {
            // Arrange
            var shippingRequest = new AddShippingRequest { Country = "USA" };
            
            // Act
            var result = await _basketService.AddShippingAsync(TestSessionId, shippingRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("USA", result.ShippingCountry);
            Assert.Equal(15.99m, result.ShippingCost);
        }
        
        [Fact]
        public async Task AddShippingAsync_CaseInsensitive_ShouldWorkCorrectly()
        {
            // Arrange
            var shippingRequest = new AddShippingRequest { Country = "uk" }; // lowercase
            
            // Act
            var result = await _basketService.AddShippingAsync(TestSessionId, shippingRequest);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("uk", result.ShippingCountry);
            Assert.Equal(5.99m, result.ShippingCost);
        }
        
        #endregion
        
        #region Calculation Tests
        
        [Fact]
        public async Task BasketCalculations_CompleteScenario_ShouldCalculateCorrectly()
        {
            // Arrange - Add items
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 1 }); // Laptop: 999.99
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 2 }); // Mouse: 25.50 * 2 = 51.00
            
            // Add discount
            await _basketService.AddDiscountCodeAsync(TestSessionId, new AddDiscountCodeRequest { DiscountCode = "SAVE10" });
            
            // Add shipping
            await _basketService.AddShippingAsync(TestSessionId, new AddShippingRequest { Country = "UK" });
            
            // Act
            var result = await _basketService.GetBasketAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            
            // Subtotal: 999.99 + 51.00 = 1050.99
            Assert.Equal(1050.99m, result.Subtotal);
            
            // Discount: 10% of 1050.99 = 105.099
            Assert.Equal(105.099m, result.DiscountAmount);
            
            // Shipping cost
            Assert.Equal(5.99m, result.ShippingCost);
            
            // Total without VAT: 1050.99 - 105.099 + 5.99 = 951.881
            Assert.Equal(951.881m, result.TotalWithoutVat);
            
            // VAT: 20% of 951.881 = 190.3762
            Assert.Equal(190.3762m, result.VatAmount);
            
            // Total with VAT: 951.881 + 190.3762 = 1142.2572
            Assert.Equal(1142.2572m, result.TotalWithVat);
        }
        
        [Fact]
        public async Task BasketCalculations_WithDiscountedProduct_ShouldNotApplyBasketDiscountToDiscountedItems()
        {
            // Arrange - Add regular item and discounted item
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 1 }); // Mouse: 25.50 (regular)
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 4, Quantity = 1 }); // Discounted Product: 100.00 (discounted)
            
            // Add basket discount
            await _basketService.AddDiscountCodeAsync(TestSessionId, new AddDiscountCodeRequest { DiscountCode = "SAVE10" });
            
            // Act
            var result = await _basketService.GetBasketAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            
            // Subtotal: 25.50 + 100.00 = 125.50
            Assert.Equal(125.50m, result.Subtotal);
            
            // Discount should only apply to non-discounted items (Mouse: 25.50)
            // 10% of 25.50 = 2.55
            Assert.Equal(2.55m, result.DiscountAmount);
        }
        
        #endregion
        
        #region Clear Basket Tests
        
        [Fact]
        public async Task ClearBasketAsync_WithItems_ShouldRemoveAllItemsAndBasket()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 2 });
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 1 });
            await _basketService.AddDiscountCodeAsync(TestSessionId, new AddDiscountCodeRequest { DiscountCode = "SAVE10" });
            
            // Act
            var result = await _basketService.ClearBasketAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(TestSessionId, result.SessionId);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Subtotal);
            Assert.Equal(0, result.TotalWithVat);
            Assert.Null(result.DiscountCode);
            
            // Verify basket is actually deleted from database
            var basketExists = await _context.Baskets.AnyAsync(b => b.SessionId == TestSessionId);
            Assert.False(basketExists);
        }
        
        [Fact]
        public async Task ClearBasketAsync_NonExistentBasket_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _basketService.ClearBasketAsync("non-existent-session")
            );
        }
        
        #endregion
        
        #region GetTotalCost Tests
        
        [Fact]
        public async Task GetTotalCostAsync_ShouldReturnBasketWithCalculations()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 2 });
            
            // Act
            var result = await _basketService.GetTotalCostAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(51.00m, result.Subtotal); // 25.50 * 2
            Assert.True(result.TotalWithVat > 0);
        }
        
        [Fact]
        public async Task GetTotalCostWithoutVatAsync_ShouldReturnBasketWithCalculations()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 2 });
            
            // Act
            var result = await _basketService.GetTotalCostWithoutVatAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(51.00m, result.Subtotal); // 25.50 * 2
            Assert.Equal(51.00m, result.TotalWithoutVat);
        }
        
        #endregion
        
        #region Edge Cases and Integration Tests
        
        [Fact]
        public async Task EmptyBasket_ShouldHaveZeroTotals()
        {
            // Act
            var result = await _basketService.GetBasketAsync(TestSessionId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0m, result.Subtotal);
            Assert.Equal(0m, result.DiscountAmount);
            Assert.Equal(0m, result.TotalWithoutVat);
            Assert.Equal(0m, result.VatAmount);
            Assert.Equal(0m, result.TotalWithVat);
        }
        
        [Fact]
        public async Task ConcurrentSessions_ShouldMaintainSeparateBaskets()
        {
            // Arrange
            const string session1 = "session-1";
            const string session2 = "session-2";
            
            // Act
            await _basketService.AddItemAsync(session1, new AddItemRequest { ProductId = 1, Quantity = 1 });
            await _basketService.AddItemAsync(session2, new AddItemRequest { ProductId = 2, Quantity = 2 });
            
            var basket1 = await _basketService.GetBasketAsync(session1);
            var basket2 = await _basketService.GetBasketAsync(session2);
            
            // Assert
            Assert.Equal(1, basket1.Items.Count);
            Assert.Equal(1, basket1.Items[0].ProductId);
            
            Assert.Equal(1, basket2.Items.Count);
            Assert.Equal(2, basket2.Items[0].ProductId);
        }
        
        #endregion
    }
} 

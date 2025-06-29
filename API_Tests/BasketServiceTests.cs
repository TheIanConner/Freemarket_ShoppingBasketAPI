using Microsoft.EntityFrameworkCore;
using ShoppingBasketAPI.Data;
using ShoppingBasketAPI.DTOs;
using ShoppingBasketAPI.Models;
using ShoppingBasketAPI.Services;

namespace Tests
{
    [TestClass]
    public class BasketServiceTests
    {
        private ShoppingBasketContext _context = null!;
        private BasketService _basketService = null!;
        private const string TestSessionId = "test-session-123";
        
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ShoppingBasketContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new ShoppingBasketContext(options);
            _basketService = new BasketService(_context);
            
            SeedTestData();
        }
        
        [TestCleanup]
        public void Cleanup()
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
        
        [TestMethod]
        public async Task GetBasketAsync_NewSession_ShouldCreateEmptyBasket()
        {
            // Act
            var result = await _basketService.GetBasketAsync(TestSessionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TestSessionId, result.SessionId);
            Assert.AreEqual(0, result.Items.Count);
            Assert.AreEqual(0, result.Subtotal);
            Assert.AreEqual(0, result.TotalWithVat);
            Assert.IsNull(result.DiscountCode);
            Assert.IsNull(result.ShippingCountry);
        }
        
        [TestMethod]
        public async Task GetBasketAsync_ExistingSession_ShouldReturnExistingBasket()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 2 });
            
            // Act
            var result = await _basketService.GetBasketAsync(TestSessionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TestSessionId, result.SessionId);
            Assert.AreEqual(1, result.Items.Count);
            Assert.AreEqual(2, result.Items[0].Quantity);
        }
        
        #endregion
        
        #region AddItemAsync Tests
        
        [TestMethod]
        public async Task AddItemAsync_NewItem_ShouldAddItemToBasket()
        {
            // Arrange
            var request = new AddItemRequest { ProductId = 1, Quantity = 2 };
            
            // Act
            var result = await _basketService.AddItemAsync(TestSessionId, request);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Items.Count);
            Assert.AreEqual(1, result.Items[0].ProductId);
            Assert.AreEqual("Laptop", result.Items[0].ProductName);
            Assert.AreEqual(2, result.Items[0].Quantity);
            Assert.AreEqual(999.99m, result.Items[0].UnitPrice);
            Assert.AreEqual(1999.98m, result.Items[0].TotalPrice);
        }
        
        [TestMethod]
        public async Task AddItemAsync_ExistingItem_ShouldIncreaseQuantity()
        {
            // Arrange
            var initialRequest = new AddItemRequest { ProductId = 1, Quantity = 2 };
            var additionalRequest = new AddItemRequest { ProductId = 1, Quantity = 3 };
            
            // Act
            await _basketService.AddItemAsync(TestSessionId, initialRequest);
            var result = await _basketService.AddItemAsync(TestSessionId, additionalRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Items.Count);
            Assert.AreEqual(5, result.Items[0].Quantity); // 2 + 3
            Assert.AreEqual(4999.95m, result.Items[0].TotalPrice); // 5 * 999.99
        }
        
        [TestMethod]
        public async Task AddItemAsync_InvalidProduct_ShouldThrowException()
        {
            // Arrange
            var request = new AddItemRequest { ProductId = 999, Quantity = 1 };
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _basketService.AddItemAsync(TestSessionId, request)
            );
        }
        
        [TestMethod]
        public async Task AddItemAsync_InactiveProduct_ShouldThrowException()
        {
            // Arrange
            var request = new AddItemRequest { ProductId = 5, Quantity = 1 }; // Product 5 is inactive
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _basketService.AddItemAsync(TestSessionId, request)
            );
        }
        
        #endregion
        
        #region AddMultipleItemsAsync Tests
        
        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Items.Count);
            
            var laptopItem = result.Items.First(i => i.ProductId == 1);
            Assert.AreEqual(2, laptopItem.Quantity);
            
            var mouseItem = result.Items.First(i => i.ProductId == 2);
            Assert.AreEqual(3, mouseItem.Quantity);
            
            var keyboardItem = result.Items.First(i => i.ProductId == 3);
            Assert.AreEqual(1, keyboardItem.Quantity);
        }
        
        [TestMethod]
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
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Items.Count);
            
            var laptopItem = result.Items.First(i => i.ProductId == 1);
            Assert.AreEqual(3, laptopItem.Quantity); // 1 + 2
            
            var mouseItem = result.Items.First(i => i.ProductId == 2);
            Assert.AreEqual(1, mouseItem.Quantity);
        }
        
        #endregion
        
        #region RemoveItemAsync Tests
        
        [TestMethod]
        public async Task RemoveItemAsync_PartialQuantity_ShouldReduceQuantity()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 5 });
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 2 };
            
            // Act
            var result = await _basketService.RemoveItemAsync(TestSessionId, removeRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Items.Count);
            Assert.AreEqual(3, result.Items[0].Quantity); // 5 - 2
        }
        
        [TestMethod]
        public async Task RemoveItemAsync_FullQuantity_ShouldRemoveItem()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 3 });
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 3 };
            
            // Act
            var result = await _basketService.RemoveItemAsync(TestSessionId, removeRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Items.Count);
        }
        
        [TestMethod]
        public async Task RemoveItemAsync_QuantityExceedsAvailable_ShouldRemoveItem()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 2 });
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 5 };
            
            // Act
            var result = await _basketService.RemoveItemAsync(TestSessionId, removeRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Items.Count);
        }
        
        [TestMethod]
        public async Task RemoveItemAsync_NoQuantitySpecified_ShouldRemoveAllOfItem()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 5 });
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = null };
            
            // Act
            var result = await _basketService.RemoveItemAsync(TestSessionId, removeRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Items.Count);
        }
        
        [TestMethod]
        public async Task RemoveItemAsync_NonExistentItem_ShouldThrowException()
        {
            // Arrange
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 1 };
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _basketService.RemoveItemAsync(TestSessionId, removeRequest)
            );
        }
        
        [TestMethod]
        public async Task RemoveItemAsync_NonExistentBasket_ShouldThrowException()
        {
            // Arrange
            var removeRequest = new RemoveItemRequest { ProductId = 1, Quantity = 1 };
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _basketService.RemoveItemAsync("non-existent-session", removeRequest)
            );
        }
        
        #endregion
        
        #region Discount Code Tests
        
        [TestMethod]
        public async Task AddDiscountCodeAsync_ValidCode_ShouldApplyDiscount()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 1 });
            var discountRequest = new AddDiscountCodeRequest { DiscountCode = "SAVE10" };
            
            // Act
            var result = await _basketService.AddDiscountCodeAsync(TestSessionId, discountRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SAVE10", result.DiscountCode);
            Assert.AreEqual(10.0m, result.DiscountPercentage);
            Assert.AreEqual(99.999m, result.DiscountAmount); // 10% of 999.99
        }
        
        [TestMethod]
        public async Task AddDiscountCodeAsync_InvalidCode_ShouldThrowException()
        {
            // Arrange
            var discountRequest = new AddDiscountCodeRequest { DiscountCode = "INVALID" };
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _basketService.AddDiscountCodeAsync(TestSessionId, discountRequest)
            );
        }
        
        [TestMethod]
        public async Task AddDiscountCodeAsync_Save20Code_ShouldApply20PercentDiscount()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 2 }); // Mouse: 25.50 * 2 = 51.00
            var discountRequest = new AddDiscountCodeRequest { DiscountCode = "SAVE20" };
            
            // Act
            var result = await _basketService.AddDiscountCodeAsync(TestSessionId, discountRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("SAVE20", result.DiscountCode);
            Assert.AreEqual(20.0m, result.DiscountPercentage);
            Assert.AreEqual(10.20m, result.DiscountAmount); // 20% of 51.00
        }
        
        [TestMethod]
        public async Task ClearDiscountCodeAsync_WithDiscount_ShouldRemoveDiscount()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 1 });
            await _basketService.AddDiscountCodeAsync(TestSessionId, new AddDiscountCodeRequest { DiscountCode = "SAVE10" });
            
            // Act
            var result = await _basketService.ClearDiscountCodeAsync(TestSessionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.DiscountCode);
            Assert.IsNull(result.DiscountPercentage);
            Assert.AreEqual(0m, result.DiscountAmount);
        }
        
        [TestMethod]
        public async Task ClearDiscountCodeAsync_NonExistentBasket_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _basketService.ClearDiscountCodeAsync("non-existent-session")
            );
        }
        
        #endregion
        
        #region Shipping Tests
        
        [TestMethod]
        public async Task AddShippingAsync_UKShipping_ShouldApplyUKRate()
        {
            // Arrange
            var shippingRequest = new AddShippingRequest { Country = "UK" };
            
            // Act
            var result = await _basketService.AddShippingAsync(TestSessionId, shippingRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("UK", result.ShippingCountry);
            Assert.AreEqual(5.99m, result.ShippingCost);
        }
        
        [TestMethod]
        public async Task AddShippingAsync_InternationalShipping_ShouldApplyInternationalRate()
        {
            // Arrange
            var shippingRequest = new AddShippingRequest { Country = "USA" };
            
            // Act
            var result = await _basketService.AddShippingAsync(TestSessionId, shippingRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("USA", result.ShippingCountry);
            Assert.AreEqual(15.99m, result.ShippingCost);
        }
        
        [TestMethod]
        public async Task AddShippingAsync_CaseInsensitive_ShouldWorkCorrectly()
        {
            // Arrange
            var shippingRequest = new AddShippingRequest { Country = "uk" }; // lowercase
            
            // Act
            var result = await _basketService.AddShippingAsync(TestSessionId, shippingRequest);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("uk", result.ShippingCountry);
            Assert.AreEqual(5.99m, result.ShippingCost);
        }
        
        #endregion
        
        #region Calculation Tests
        
        [TestMethod]
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
            Assert.IsNotNull(result);
            
            // Subtotal: 999.99 + 51.00 = 1050.99
            Assert.AreEqual(1050.99m, result.Subtotal);
            
            // Discount: 10% of 1050.99 = 105.099
            Assert.AreEqual(105.099m, result.DiscountAmount);
            
            // Shipping cost
            Assert.AreEqual(5.99m, result.ShippingCost);
            
            // Total without VAT: 1050.99 - 105.099 + 5.99 = 951.881
            Assert.AreEqual(951.881m, result.TotalWithoutVat);
            
            // VAT: 20% of 951.881 = 190.3762
            Assert.AreEqual(190.3762m, result.VatAmount);
            
            // Total with VAT: 951.881 + 190.3762 = 1142.2572
            Assert.AreEqual(1142.2572m, result.TotalWithVat);
        }
        
        [TestMethod]
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
            Assert.IsNotNull(result);
            
            // Subtotal: 25.50 + 100.00 = 125.50
            Assert.AreEqual(125.50m, result.Subtotal);
            
            // Discount should only apply to non-discounted items (Mouse: 25.50)
            // 10% of 25.50 = 2.55
            Assert.AreEqual(2.55m, result.DiscountAmount);
        }
        
        #endregion
        
        #region Clear Basket Tests
        
        [TestMethod]
        public async Task ClearBasketAsync_WithItems_ShouldRemoveAllItemsAndBasket()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 1, Quantity = 2 });
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 1 });
            await _basketService.AddDiscountCodeAsync(TestSessionId, new AddDiscountCodeRequest { DiscountCode = "SAVE10" });
            
            // Act
            var result = await _basketService.ClearBasketAsync(TestSessionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(TestSessionId, result.SessionId);
            Assert.AreEqual(0, result.Items.Count);
            Assert.AreEqual(0, result.Subtotal);
            Assert.AreEqual(0, result.TotalWithVat);
            Assert.IsNull(result.DiscountCode);
            
            // Verify basket is actually deleted from database
            var basketExists = await _context.Baskets.AnyAsync(b => b.SessionId == TestSessionId);
            Assert.IsFalse(basketExists);
        }
        
        [TestMethod]
        public async Task ClearBasketAsync_NonExistentBasket_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await _basketService.ClearBasketAsync("non-existent-session")
            );
        }
        
        #endregion
        
        #region GetTotalCost Tests
        
        [TestMethod]
        public async Task GetTotalCostAsync_ShouldReturnBasketWithCalculations()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 2 });
            
            // Act
            var result = await _basketService.GetTotalCostAsync(TestSessionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(51.00m, result.Subtotal); // 25.50 * 2
            Assert.IsTrue(result.TotalWithVat > 0);
        }
        
        [TestMethod]
        public async Task GetTotalCostWithoutVatAsync_ShouldReturnBasketWithCalculations()
        {
            // Arrange
            await _basketService.AddItemAsync(TestSessionId, new AddItemRequest { ProductId = 2, Quantity = 2 });
            
            // Act
            var result = await _basketService.GetTotalCostWithoutVatAsync(TestSessionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(51.00m, result.Subtotal); // 25.50 * 2
            Assert.AreEqual(51.00m, result.TotalWithoutVat);
        }
        
        #endregion
        
        #region Edge Cases and Integration Tests
        
        [TestMethod]
        public async Task EmptyBasket_ShouldHaveZeroTotals()
        {
            // Act
            var result = await _basketService.GetBasketAsync(TestSessionId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Items.Count);
            Assert.AreEqual(0m, result.Subtotal);
            Assert.AreEqual(0m, result.DiscountAmount);
            Assert.AreEqual(0m, result.TotalWithoutVat);
            Assert.AreEqual(0m, result.VatAmount);
            Assert.AreEqual(0m, result.TotalWithVat);
        }
        
        [TestMethod]
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
            Assert.AreEqual(1, basket1.Items.Count);
            Assert.AreEqual(1, basket1.Items[0].ProductId);
            
            Assert.AreEqual(1, basket2.Items.Count);
            Assert.AreEqual(2, basket2.Items[0].ProductId);
        }
        
        #endregion
    }
} 

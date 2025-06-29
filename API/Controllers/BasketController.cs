using Microsoft.AspNetCore.Mvc;
using ShoppingBasketAPI.DTOs;
using ShoppingBasketAPI.Services;

namespace ShoppingBasketAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketService _basketService;

        public BasketController(IBasketService basketService)
        {
            _basketService = basketService;
        }

        /// <summary>
        /// Get the current basket for a session
        /// </summary>
        [HttpGet("{sessionId}")]
        public async Task<ActionResult<BasketResponse>> GetBasket(string sessionId)
        {
            try
            {
                var basket = await _basketService.GetBasketAsync(sessionId);
                return Ok(basket);
            }
            catch
            {
                return BadRequest(new { error = "An error occurred while retrieving the basket" });
            }
        }

        /// <summary>
        /// Add a single item to the basket
        /// </summary>
        [HttpPost("{sessionId}/items")]
        public async Task<ActionResult<BasketResponse>> AddItem(string sessionId, [FromBody] AddItemRequest request)
        {
            try
            {
                var basket = await _basketService.AddItemAsync(sessionId, request);
                return Ok(basket);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { error = "An error occurred while adding the item" });
            }
        }

        /// <summary>
        /// Add multiple items to the basket
        /// </summary>
        [HttpPost("{sessionId}/items/multiple")]
        public async Task<ActionResult<BasketResponse>> AddMultipleItems(string sessionId, [FromBody] AddMultipleItemsRequest request)
        {
            try
            {
                var basket = await _basketService.AddMultipleItemsAsync(sessionId, request);
                return Ok(basket);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { error = "An error occurred while adding the items" });
            }
        }

        /// <summary>
        /// Remove an item from the basket
        /// </summary>
        [HttpDelete("{sessionId}/items")]
        public async Task<ActionResult<BasketResponse>> RemoveItem(string sessionId, [FromBody] RemoveItemRequest request)
        {
            try
            {
                var basket = await _basketService.RemoveItemAsync(sessionId, request);
                return Ok(basket);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { error = "An error occurred while removing the item" });
            }
        }

        /// <summary>
        /// Add a discount code to the basket
        /// </summary>
        [HttpPost("{sessionId}/discount")]
        public async Task<ActionResult<BasketResponse>> AddDiscountCode(string sessionId, [FromBody] AddDiscountCodeRequest request)
        {
            try
            {
                var basket = await _basketService.AddDiscountCodeAsync(sessionId, request);
                return Ok(basket);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { error = "An error occurred while applying the discount code" });
            }
        }

        /// <summary>
        /// Clear discount code from the basket
        /// </summary>
        [HttpDelete("{sessionId}/discount")]
        public async Task<ActionResult<BasketResponse>> ClearDiscountCode(string sessionId)
        {
            try
            {
                var basket = await _basketService.ClearDiscountCodeAsync(sessionId);
                return Ok(basket);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { error = "An error occurred while clearing the discount code" });
            }
        }

        /// <summary>
        /// Add shipping information to the basket
        /// </summary>
        [HttpPost("{sessionId}/shipping")]
        public async Task<ActionResult<BasketResponse>> AddShipping(string sessionId, [FromBody] AddShippingRequest request)
        {
            try
            {
                var basket = await _basketService.AddShippingAsync(sessionId, request);
                return Ok(basket);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { error = "An error occurred while adding shipping information" });
            }
        }

        /// <summary>
        /// Get the total cost including VAT
        /// </summary>
        [HttpGet("{sessionId}/total")]
        public async Task<ActionResult<BasketResponse>> GetTotalCost(string sessionId)
        {
            try
            {
                var basket = await _basketService.GetTotalCostAsync(sessionId);
                return Ok(basket);
            }
            catch
            {
                return BadRequest(new { error = "An error occurred while calculating the total" });
            }
        }

        /// <summary>
        /// Get the total cost without VAT
        /// </summary>
        [HttpGet("{sessionId}/total-without-vat")]
        public async Task<ActionResult<BasketResponse>> GetTotalCostWithoutVat(string sessionId)
        {
            try
            {
                var basket = await _basketService.GetTotalCostWithoutVatAsync(sessionId);
                return Ok(basket);
            }
            catch
            {
                return BadRequest(new { error = "An error occurred while calculating the total without VAT" });
            }
        }

        /// <summary>
        /// Clear all items from the basket
        /// </summary>
        [HttpDelete("{sessionId}")]
        public async Task<ActionResult<BasketResponse>> ClearBasket(string sessionId)
        {
            try
            {
                var basket = await _basketService.ClearBasketAsync(sessionId);
                return Ok(basket);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { error = "An error occurred while clearing the basket" });
            }
        }
    }
} 
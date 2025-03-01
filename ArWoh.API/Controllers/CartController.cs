using ArWoh.API.DTOs.CartDTOs;
using ArWoh.API.DTOs.CartItemDTOs;
using ArWoh.API.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Ultils;

namespace ArWoh.API.Controllers
{
    [ApiController]
    [Route("api/carts")]

    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILoggerService _loggerService;
        public CartController(ICartService cartService, ILoggerService loggerService)
        {
            _cartService = cartService;
            _loggerService = loggerService;
        }

        [HttpPost("{userId}")]
        [ProducesResponseType(typeof(ApiResult<CartDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemDto addCartItemDto, int userId)
        {
            try
            {
                _loggerService.Info($"Adding item to cart for user {userId}");

                var updatedCart = await _cartService.CreateCartAsync(addCartItemDto, userId);

                return Ok(ApiResult<CartDto>.Success(updatedCart));
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Unexpected error in AddToCart: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Error("An error occurred while adding item to the cart"));
            }
        }

        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(ApiResult<CartDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetCartByUserId(int userId)
        {
            try
            {
                _loggerService.Info($"Fetching cart for user {userId}");

                var cart = await _cartService.GetCartByUserId(userId);

                if (cart == null)
                {
                    _loggerService.Warn($"No cart found for user {userId}");
                    return NotFound(ApiResult<object>.Error("Cart not found"));
                }

                return Ok(ApiResult<CartDto>.Success(cart));
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Unexpected error in GetCartByUserId: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred"));
            }
        }

        [HttpPut("{userId}")]
        [ProducesResponseType(typeof(ApiResult<CartDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> UpdateCart([FromBody] UpdateCartItemDto updateCartItemDto, int userId)
        {
            try
            {
                _loggerService.Info($"Updating cart item for user {userId}");

                var updatedCart = await _cartService.UpdateCartAsync(updateCartItemDto, userId);

                return Ok(ApiResult<CartDto>.Success(updatedCart));
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Unexpected error in UpdateCart: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Error("An error occurred while updating the cart"));
            }
        }

        [HttpDelete("{userId}/{cartItemId}")]
        [ProducesResponseType(typeof(ApiResult<CartDto>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> RemoveFromCart(int cartItemId, int userId)
        {
            try
            {
                _loggerService.Info($"Removing item from cart for user {userId}");

                var updatedCart = await _cartService.DeleteCartItemAsync(cartItemId, userId);

                return Ok(ApiResult<CartDto>.Success(updatedCart));
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Unexpected error in RemoveFromCart: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Error("An error occurred while removing item from the cart"));
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<CartDto>>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> GetAllCarts()
        {
            try
            {
                _loggerService.Info("Fetching all carts");

                var carts = await _cartService.GetAllCarts();

                if (!carts.Any())
                {
                    _loggerService.Warn("No carts found.");
                    return Ok(ApiResult<IEnumerable<CartDto>>.Success(new List<CartDto>()));
                }

                return Ok(ApiResult<IEnumerable<CartDto>>.Success(carts));
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Unexpected error in GetAllCarts: {ex.Message}");
                return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred"));
            }
        }
    }
}

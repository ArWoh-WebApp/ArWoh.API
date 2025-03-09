using ArWoh.API.DTOs.CartDTOs;
using ArWoh.API.DTOs.CartItemDTOs;
using ArWoh.API.Interface;
using ArWoh.API.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArWoh.API.Controllers;

[ApiController]
[Route("api/carts")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILoggerService _loggerService;
    private readonly IClaimService _claimService;

    public CartController(ICartService cartService, ILoggerService loggerService, IClaimService claimService)
    {
        _cartService = cartService;
        _loggerService = loggerService;
        _claimService = claimService;
    }

    [HttpPost()]
    [ProducesResponseType(typeof(ApiResult<CartDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> AddToCart([FromBody] AddCartItemDto addCartItemDto)
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
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

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResult<CartDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetCartByUserId()
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
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


    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResult<CartDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateCart([FromBody] UpdateCartItemDto updateCartItemDto)
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();

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

    [HttpDelete("me/{cartItemId}")]
    [ProducesResponseType(typeof(ApiResult<CartDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]

    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        try
        {
            var userId = _claimService.GetCurrentUserId();
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
}
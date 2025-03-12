using ArWoh.API.DTOs.CartDTOs;
using ArWoh.API.DTOs.CartItemDTOs;

namespace ArWoh.API.Interface;

public interface ICartService
{
    Task<CartDto> GetCartByUserId(int userId);
    Task<CartDto> CreateCartAsync(AddCartItemDto addCartItemDto, int userId);
    Task<CartDto> UpdateCartAsync(UpdateCartItemDto updateCartItemDto, int userId);
    Task<CartDto> DeleteCartItemAsync(int cartItemId, int userId);
    Task<bool> ResetCartAfterPayment(int userId);
}
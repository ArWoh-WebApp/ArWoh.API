using ArWoh.API.DTOs.CartDTOs;
using ArWoh.API.DTOs.CartItemDTOs;
using ArWoh.API.Entities;
using ArWoh.API.Enums;
using ArWoh.API.Interface;
using ArWoh.API.Service.ThirdPartyService.Interfaces;
using ArWoh.API.Service.ThirdPartyService.Types;
using Microsoft.EntityFrameworkCore;
using Net.payOS.Types;

namespace ArWoh.API.Service;

public class CartService : ICartService
{
    private readonly ILoggerService _loggerService;
    private readonly IUnitOfWork _unitOfWork;

    public CartService(ILoggerService loggerService, IUnitOfWork unitOfWork)
    {
        _loggerService = loggerService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Thêm ảnh vào giỏ hàng
    /// </summary>
    /// <param name="addCartItemDto"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<CartDto> CreateCartAsync(AddCartItemDto addCartItemDto, int userId)
    {
        try
        {
            _loggerService.Info($"Adding image {addCartItemDto.ImageId} to cart for user {userId}");

            // Kiểm tra xem giỏ hàng của người dùng đã tồn tại chưa
            var cart = await _unitOfWork.Carts
                .GetQueryable()
                .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
                await _unitOfWork.Carts.AddAsync(cart);
                await _unitOfWork.CompleteAsync();
            }

            // Kiểm tra xem ảnh đã tồn tại trong giỏ chưa
            var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ImageId == addCartItemDto.ImageId);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += addCartItemDto.Quantity; // Nếu có, cộng thêm số lượng
                _unitOfWork.CartItems.Update(existingCartItem);
            }
            else
            {
                var image = await _unitOfWork.Images.GetByIdAsync(addCartItemDto.ImageId);
                if (image == null) throw new Exception("Image not found");

                var newCartItem = new CartItem
                {
                    ImageId = addCartItemDto.ImageId,
                    ImageTitle = image.Title,
                    Quantity = addCartItemDto.Quantity,
                    Price = image.Price,
                    CartId = cart.Id
                };

                await _unitOfWork.CartItems.AddAsync(newCartItem);
            }

            await _unitOfWork.CompleteAsync();

            return await GetCartByUserId(userId); // Trả về giỏ hàng mới cập nhật
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in CreateCart: {ex.Message}");
            throw new Exception("An error occurred while adding item to the cart.", ex);
        }
    }

    /// <summary>
    /// Cập nhật số lượng ảnh trong giỏ hàng
    /// </summary>
    /// <param name="updateCartItemDto"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<CartDto> UpdateCartAsync(UpdateCartItemDto updateCartItemDto, int userId)
    {
        try
        {
            _loggerService.Info($"Updating cart item with ID {updateCartItemDto.CartItemId} for user {userId}");

            var cart = await _unitOfWork.Carts
                .GetQueryable()
                .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                throw new KeyNotFoundException("Cart not found for this user.");

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == updateCartItemDto.CartItemId);

            if (cartItem == null)
                throw new KeyNotFoundException("Cart item not found.");

            // Nếu quantity == 0, xóa ảnh khỏi giỏ
            if (updateCartItemDto.Quantity == 0)
            {
                cart.CartItems.Remove(cartItem);
                _unitOfWork.CartItems.Delete(cartItem);
            }
            else
            {
                cartItem.Quantity = updateCartItemDto.Quantity; // Cập nhật số lượng
                _unitOfWork.CartItems.Update(cartItem);
            }

            await _unitOfWork.CompleteAsync();
            return await GetCartByUserId(userId);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in UpdateCart: {ex.Message}");
            throw new Exception("An error occurred while updating the cart.", ex);
        }
    }

    /// <summary>
    /// Xóa ảnh khỏi giỏ hàng
    /// </summary>
    /// <param name="cartItemId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<CartDto> DeleteCartItemAsync(int cartItemId, int userId)
    {
        try
        {
            _loggerService.Info($"Removing cart item with ID {cartItemId} for user {userId}");

            var cart = await _unitOfWork.Carts
                .GetQueryable()
                .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                throw new KeyNotFoundException("Cart not found for this user.");

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem == null)
                throw new KeyNotFoundException("Cart item not found.");

            cart.CartItems.Remove(cartItem);
            _unitOfWork.CartItems.Delete(cartItem);

            await _unitOfWork.CompleteAsync();
            return await GetCartByUserId(userId);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in DeleteCart: {ex.Message}");
            throw new Exception("An error occurred while removing item from the cart.", ex);
        }
    }

    /// <summary>
    /// Lấy danh sách ảnh trong giỏ hàng của user
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<CartDto> GetCartByUserId(int userId)
    {
        try
        {
            _loggerService.Info($"Fetching cart for user {userId}");

            var cart = await _unitOfWork.Carts
                .GetQueryable()
                .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                throw new KeyNotFoundException("Cart not found for this user.");

            var cartDto = new CartDto
            {
                UserId = cart.UserId,
                CartItems = cart.CartItems.Select(ci => new CartItemDto()
                {
                    CartItemId = ci.Id,
                    ImageId = ci.ImageId,
                    ImageTitle = ci.Image?.Title,
                    Price = ci.Price,
                    Quantity = ci.Quantity
                }).ToList(),
                TotalPrice = cart.CartItems.Sum(ci => ci.Price * ci.Quantity)
            };

            _loggerService.Success($"Successfully fetched cart for user {userId}");

            return cartDto;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Unexpected error in GetCartByUserId: {ex.Message}");
            throw new Exception("An error occurred while fetching the cart.", ex);
        }
    }
}
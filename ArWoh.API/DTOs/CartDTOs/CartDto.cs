using ArWoh.API.DTOs.CartItemDTOs;
using ArWoh.API.Entities;

namespace ArWoh.API.DTOs.CartDTOs;

public class CartDto
{
    public int UserId { get; set; }
    public List<CartItemDto> CartItems { get; set; }
    public decimal TotalPrice { get; set; }
}
﻿using ArWoh.API.DTOs.CartItemDTOs;

namespace ArWoh.API.DTOs.CartDTOs
{
    public class CartDto
    {
        public int UserId { get; set; }
        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
        public decimal TotalPrice { get; set; }
    }
}

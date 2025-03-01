﻿using ArWoh.API.DTOs.UserDTOs;

namespace ArWoh.API.Interface
{
    public interface IUserService
    {
        Task<UserProfileDto> GetUserDetailsById(int userId);
        Task<List<UserProfileDto>> GetAllUsers();
        Task<List<UserProfileDto>> GetPhotographer();
    }
}

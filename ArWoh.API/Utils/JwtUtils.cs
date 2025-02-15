﻿using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace VaccinaCare.Application.Ultils
{
    public static class JwtUtils
    {
        public static string GenerateJwtToken(int userId, string email, string role, IConfiguration configuration, TimeSpan validityPeriod)
        {
            // Kiểm tra và lấy secret key từ cấu hình
            var secretKey = configuration["JWT:SecretKey"];
            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
            {
                throw new ArgumentException("JWT SecretKey must be at least 32 characters.");
            }

            // Lấy Issuer và Audience từ cấu hình
            var issuer = configuration["JWT:Issuer"];
            var audience = configuration["JWT:Audience"];

            // Tạo signing credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Tạo claims cho token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // Thêm NameIdentifier
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };


            // Tạo mô tả token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(validityPeriod),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = signingCredentials
            };

            // Tạo token và trả về chuỗi đã được mã hóa
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

}

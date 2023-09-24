﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Enums;

namespace Topluluk.Shared.Helper
{
	public class TokenHelper
	{
		private readonly IConfiguration _configuration;

		public TokenHelper(IConfiguration configuration)
		{
			_configuration = configuration;
		}

        public static string GetUserNameByToken(HttpRequest request)
        {
            if (request == null || request.Headers == null || !request.Headers.ContainsKey("Authorization") || request.Headers["Authorization"].Count == 0)
            {
                return string.Empty;
            }
            var token = request.Headers["Authorization"][0];
            token = token.Split("Bearer ")[1];
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var username = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == Enum.GetName(typeof(TokenFieldEnum),TokenFieldEnum.NAME)|| c.Type == ClaimTypes.Name )?.Value;

            return username ?? throw new Exception($"{typeof(TokenHelper).Name}:Username not found in token");
        }
        public static string GetUserIdByToken(HttpRequest request)
        {
            if (request == null || request.Headers == null || !request.Headers.ContainsKey("Authorization") || request.Headers["Authorization"].Count == 0)
            {
                return string.Empty;
            }
            var token = request.Headers["Authorization"][0];
            token = token.Split("Bearer ")[1];
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var userId = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == Enum.GetName(typeof(TokenFieldEnum),TokenFieldEnum.ID) || c.Type == ClaimTypes.NameIdentifier)?.Value;

            return userId ?? throw new Exception($"{typeof(TokenHelper).Name}:UserId not found in token");
        }
        public static string GetUserIdByToken(string token)
        {
            if (token.IsNullOrEmpty())
            {
                return string.Empty;
            }

            var handler = new JwtSecurityTokenHandler();
            token = token.Split("Bearer ")[1];
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var userId =  jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == Enum.GetName(typeof(TokenFieldEnum),TokenFieldEnum.ID) || c.Type == ClaimTypes.NameIdentifier)?.Value;
            return userId ?? throw new Exception($"{typeof(TokenHelper).Namespace}: UserId not found in token");
        }
        public static List<string> GetUserRolesByToken(HttpRequest request)
        {
            if (request == null || request.Headers == null || !request.Headers.ContainsKey("Authorization") || request.Headers["Authorization"].Count == 0)
            {
                return new();
            }
            var token = request.Headers["Authorization"][0];
            token = token.Split("Bearer ")[1];
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var userRoles = jwtSecurityToken.Claims.Where(c => c.Type == Enum.GetName(typeof(TokenFieldEnum),TokenFieldEnum.ROLES) || c.Type == ClaimTypes.Role )?.Select(c => c.Value).ToList();

            return userRoles ?? throw new Exception($"{typeof(TokenHelper).Name}:UserId not found in token");
        }
        public static string GetToken(HttpRequest request)
        {
            if (request == null || request.Headers == null || !request.Headers.ContainsKey("Authorization") || request.Headers["Authorization"].Count == 0)
            {
                return string.Empty;
            }
            var token = request.Headers["Authorization"][0];
            return token;
        }

        public static bool GetByTokenControl(HttpRequest request)
        {
            if (request != null && request.Headers != null && request.Headers["Authorization"].Count == 0)
            {
                return false;
            }
            else
                return true;
        }

        public TokenDto CreateAccessToken( string userId, string userName, List<string> roles ,int month = 6)
		{
            SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            SigningCredentials signingCredentials = new(securityKey, SecurityAlgorithms.HmacSha256);
            var authClaims = new List<Claim>()
            {
                new(Enum.GetName(typeof(TokenFieldEnum),TokenFieldEnum.ID)!, userId),
                new(Enum.GetName(typeof(TokenFieldEnum),TokenFieldEnum.NAME)!, userName),
            };
            foreach (var userRole in roles)
            {
                authClaims.Add(new Claim(Enum.GetName(typeof(TokenFieldEnum),TokenFieldEnum.ROLES)!, userRole));
            }
            DateTime tokenExpiredAt = DateTime.UtcNow.AddMonths(month);
            JwtSecurityToken securityToken = new(
                expires: tokenExpiredAt,
                notBefore: DateTime.UtcNow,
                signingCredentials: signingCredentials,
                claims: authClaims
            );
            JwtSecurityTokenHandler tokenHandler = new();
            return new()
            {
                AccessToken = tokenHandler.WriteToken(securityToken),
                RefreshToken = CreateRefreshToken(),
                ExpiredAt = tokenExpiredAt
            };
        }

        public  string CreateRefreshToken()
        {
            byte[] number = new byte[32];
            using RandomNumberGenerator random = RandomNumberGenerator.Create();
            random.GetBytes(number);
            return Convert.ToBase64String(number);
        }



	}
}


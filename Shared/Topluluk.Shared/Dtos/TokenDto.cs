using System;
namespace Topluluk.Shared.Dtos
{
    public record struct TokenDto
    {
        public string AccessToken { get; init; }
        public string RefreshToken { get; init; }
        public DateTime ExpiredAt { get; init; }
    }
}


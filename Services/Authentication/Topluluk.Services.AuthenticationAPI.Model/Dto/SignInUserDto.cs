using System;
using Topluluk.Shared.Enums;

namespace Topluluk.Services.AuthenticationAPI.Model.Dto
{
	public record struct SignInUserDto
	{
		public string? UserName { get; init; }
		public string? Email { get; init; }
		public string Password { get; init; }
	}
}

